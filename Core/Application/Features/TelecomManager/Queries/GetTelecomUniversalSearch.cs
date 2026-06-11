using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Features.CustomerManager.Queries;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

/// <summary>Strict unified lookup hit — National ID, commercial registry, or MSISDN only.</summary>
public record TelecomUniversalSearchRowDto
{
    public string ResultType { get; init; } = null!;
    public string Id { get; init; } = null!;
    public string? SubscriberProfileId { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerNameAr { get; init; }
    public string? NationalId { get; init; }
    public string? Msisdn { get; init; }
    public string? Status { get; init; }
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
}

public class GetTelecomUniversalSearchResult
{
    public List<TelecomUniversalSearchRowDto> Data { get; init; } = new();
}

public class GetTelecomUniversalSearchRequest : IRequest<GetTelecomUniversalSearchResult>
{
    public string? Term { get; init; }
    public bool AvailableOnly { get; init; }
    public bool ProfilesOnly { get; init; }
}

public class GetTelecomUniversalSearchHandler : IRequestHandler<GetTelecomUniversalSearchRequest, GetTelecomUniversalSearchResult>
{
    private const int MaxRows = 10;

    private readonly IQueryContext _context;
    private readonly IFieldEncryptionService _encryption;
    private readonly ISubscriberAccessAuditService _subscriberAudit;

    public GetTelecomUniversalSearchHandler(
        IQueryContext context,
        IFieldEncryptionService encryption,
        ISubscriberAccessAuditService subscriberAudit)
    {
        _context = context;
        _encryption = encryption;
        _subscriberAudit = subscriberAudit;
    }

    private static string NormalizeIndicDigitsToAscii(string input)
    {
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            if (ch is >= '\u0660' and <= '\u0669')
            {
                chars[i] = (char)('0' + (ch - '\u0660'));
            }
            else if (ch is >= '\u06F0' and <= '\u06F9')
            {
                chars[i] = (char)('0' + (ch - '\u06F0'));
            }
        }

        return new string(chars);
    }

    private static bool IsLikelyNameOnlySearch(string textTerm, string digitsOnly) =>
        digitsOnly.Length < 2 && textTerm.Any(char.IsLetter);

    private static bool LooksLikeSyrianMobileDigits(string digitsOnly) =>
        digitsOnly.Length == 10 && digitsOnly.StartsWith("09", StringComparison.Ordinal);

    private static string MaskNationalId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var d = raw.Trim();
        return d.Length >= 4 ? new string('*', d.Length - 4) + d[^4..] : "****";
    }

    public async Task<GetTelecomUniversalSearchResult> Handle(
        GetTelecomUniversalSearchRequest request,
        CancellationToken cancellationToken)
    {
        var term = (request.Term ?? string.Empty).Trim();
        if (term.Length < 2)
        {
            return new GetTelecomUniversalSearchResult();
        }

        var textTerm = NormalizeIndicDigitsToAscii(term).Trim();
        var digitsOnly = TelecomPhoneNormalizer.DigitsOnly(textTerm);

        if (IsLikelyNameOnlySearch(textTerm, digitsOnly))
        {
            return new GetTelecomUniversalSearchResult();
        }

        var results = new List<TelecomUniversalSearchRowDto>();
        var canonicalMsisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(textTerm);
        var searchNationalId = digitsOnly.Length == 10 && !LooksLikeSyrianMobileDigits(digitsOnly);
        var searchCommercialRegistry = !searchNationalId
            && canonicalMsisdn == null
            && textTerm.Length >= 3
            && textTerm.Any(char.IsLetterOrDigit);
        var searchMsisdn = canonicalMsisdn != null;

        if (!request.AvailableOnly)
        {
            if (searchNationalId)
            {
                await AppendProfilesByNationalIdAsync(results, digitsOnly, cancellationToken);
            }

            if (searchCommercialRegistry)
            {
                await AppendProfilesByCommercialRegistryAsync(results, textTerm.Trim(), cancellationToken);
            }
        }

        if (!request.ProfilesOnly && searchMsisdn)
        {
            await AppendMsisdnExactHitAsync(results, canonicalMsisdn!, request.AvailableOnly, cancellationToken);
        }

        var finalRows = DeduplicateSearchRows(results).Take(MaxRows).ToList();

        var auditMatches = finalRows
            .GroupBy(r => r.CustomerId ?? $"row:{r.ResultType}:{r.Id}")
            .Select(g =>
            {
                var first = g.First();
                return new SubscriberSearchMatchAuditDto
                {
                    CustomerId = first.CustomerId ?? first.Id,
                    Name = first.CustomerNameAr ?? first.Title,
                    PhoneOrMsisdn = first.Msisdn ?? first.Title,
                };
            })
            .ToList();

        await _subscriberAudit.LogSearchAsync(
            "UnifiedSearch",
            null,
            null,
            term,
            auditMatches,
            cancellationToken);

        return new GetTelecomUniversalSearchResult { Data = finalRows };
    }

    private async Task AppendProfilesByNationalIdAsync(
        List<TelecomUniversalSearchRowDto> results,
        string nationalIdDigits,
        CancellationToken cancellationToken)
    {
        var hash = _encryption.ComputeSearchHash(nationalIdDigits);
        var customers = await _context.Customer
            .AsNoTracking()
            .IsDeletedEqualTo()
            .OfType<IndividualCustomer>()
            .Where(c => c.NationalIdSearchHash == hash)
            .Select(c => new { c.Id, c.DisplayName, c.Status })
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        if (customers.Count == 0)
        {
            return;
        }

        var customerIds = customers.Select(c => c.Id).ToList();
        var profiles = await LoadSubscriberProfilesForCustomersAsync(customerIds, cancellationToken);

        foreach (var customer in customers)
        {
            var customerProfiles = profiles.Where(p => p.CustomerId == customer.Id).ToList();
            AppendCustomerConsolidatedRow(
                results,
                customer.Id,
                customer.DisplayName,
                customer.Status,
                customerProfiles,
                nationalIdDigits);
        }
    }

    private async Task AppendProfilesByCommercialRegistryAsync(
        List<TelecomUniversalSearchRowDto> results,
        string registryTerm,
        CancellationToken cancellationToken)
    {
        var normalizedRegistry = registryTerm.Trim();
        var profiles = await LoadSubscriberProfilesAsync(
            p => p.Customer != null
                 && EF.Property<CustomerKind>(p.Customer, nameof(Customer.CustomerKind)) == CustomerKind.Corporate
                 && EF.Property<string>(p.Customer, nameof(CorporateCustomer.CommercialRegistryNumber)) != null
                 && EF.Property<string>(p.Customer, nameof(CorporateCustomer.CommercialRegistryNumber)) == normalizedRegistry,
            cancellationToken);

        foreach (var group in profiles.GroupBy(p => p.CustomerId))
        {
            var anchor = group.First();
            var customer = anchor.Customer;
            AppendCustomerConsolidatedRow(
                results,
                group.Key,
                customer?.DisplayName,
                customer?.Status ?? CustomerStatus.Active,
                group.ToList(),
                commercialRegistry: normalizedRegistry);
        }
    }

    private async Task AppendMsisdnExactHitAsync(
        List<TelecomUniversalSearchRowDto> results,
        string canonicalMsisdn,
        bool availableOnly,
        CancellationToken cancellationToken)
    {
        var query = _context.MsisdnAsset
            .AsNoTracking()
            .Include(m => m.SubscriberProfile!)
                .ThenInclude(sp => sp!.Customer)
            .Include(m => m.SubscriberProfile!)
                .ThenInclude(sp => sp!.Subscriptions)
                    .ThenInclude(s => s.MsisdnAsset)
            .Include(m => m.SubscriberProfile!)
                .ThenInclude(sp => sp!.Subscriptions)
                    .ThenInclude(s => s.SubscriptionTypeLookup)
            .AsSplitQuery()
            .Where(x => !x.IsDeleted && x.Msisdn == canonicalMsisdn);

        if (availableOnly)
        {
            query = query.Where(x => x.PoolStatus == MsisdnPoolStatus.Available);
        }
        else
        {
            query = query.Where(x =>
                x.SubscriberProfileId != null || x.PoolStatus != MsisdnPoolStatus.Available);
        }

        var assets = await query.Take(MaxRows).ToListAsync(cancellationToken);

        foreach (var m in assets)
        {
            if (m.SubscriberProfileId != null && results.Any(r => r.SubscriberProfileId == m.SubscriberProfileId))
            {
                continue;
            }

            if (m.SubscriberProfile != null)
            {
                AppendProfileRows(results, [m.SubscriberProfile], null, null, m.Msisdn);
                continue;
            }

            results.Add(new TelecomUniversalSearchRowDto
            {
                ResultType = "Msisdn",
                Id = m.Id,
                Msisdn = m.Msisdn,
                Title = m.Msisdn,
                Subtitle = m.PoolStatus.ToString(),
            });
        }
    }

    private async Task<List<SubscriberProfile>> LoadSubscriberProfilesAsync(
        System.Linq.Expressions.Expression<Func<SubscriberProfile, bool>> predicate,
        CancellationToken cancellationToken)
    {
        return await _context.SubscriberProfile
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Subscriptions)
                .ThenInclude(s => s.MsisdnAsset)
            .Include(x => x.Subscriptions)
                .ThenInclude(s => s.SubscriptionTypeLookup)
            .AsSplitQuery()
            .Where(x => !x.IsDeleted)
            .Where(predicate)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<SubscriberProfile>> LoadSubscriberProfilesForCustomersAsync(
        IReadOnlyList<string> customerIds,
        CancellationToken cancellationToken)
    {
        if (customerIds.Count == 0)
        {
            return [];
        }

        return await _context.SubscriberProfile
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Subscriptions)
                .ThenInclude(s => s.MsisdnAsset)
            .Include(x => x.Subscriptions)
                .ThenInclude(s => s.SubscriptionTypeLookup)
            .AsSplitQuery()
            .Where(x => !x.IsDeleted && customerIds.Contains(x.CustomerId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Identity search: one row per customer with primary MSISDN — lines live on Customer 360.</summary>
    private static void AppendCustomerConsolidatedRow(
        List<TelecomUniversalSearchRowDto> results,
        string customerId,
        string? displayName,
        CustomerStatus status,
        IReadOnlyList<SubscriberProfile> profiles,
        string? nationalIdDigits = null,
        string? commercialRegistry = null)
    {
        if (results.Any(r => r.CustomerId == customerId))
        {
            return;
        }

        var lineHits = new List<(string Msisdn, string ProfileId, bool IsPrimary, bool IsCanonical)>();
        foreach (var profile in profiles)
        {
            var subs = GetCustomer360Handler.DeduplicateSubscriptionsByLine(
                profile.Subscriptions.Where(s => !s.IsDeleted).ToList());
            foreach (var sub in subs)
            {
                var msisdn = sub.MsisdnAsset?.Msisdn?.Trim();
                if (string.IsNullOrEmpty(msisdn))
                {
                    continue;
                }

                lineHits.Add((
                    msisdn,
                    profile.Id,
                    sub.IsPrimaryLine,
                    sub.MsisdnAsset?.SubscriberProfileId == profile.Id));
            }
        }

        var distinctLines = lineHits
            .GroupBy(x => x.Msisdn, StringComparer.Ordinal)
            .Select(g => g
                .OrderByDescending(x => x.IsCanonical)
                .ThenByDescending(x => x.IsPrimary)
                .First())
            .ToList();

        if (distinctLines.Count == 0)
        {
            if (profiles.Count == 0)
            {
                results.Add(BuildCustomerOnlyRow(customerId, displayName, status, nationalIdDigits, null));
            }

            return;
        }

        var primary = distinctLines
            .OrderByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.IsCanonical)
            .First();

        string? nationalMasked = nationalIdDigits != null ? MaskNationalId(nationalIdDigits) : null;
        if (nationalMasked == null && profiles.FirstOrDefault()?.Customer is IndividualCustomer ind)
        {
            nationalMasked = MaskNationalId(ind.NationalId);
        }

        var registry = commercialRegistry;
        if (registry == null && profiles.FirstOrDefault()?.Customer is CorporateCustomer corp)
        {
            registry = corp.CommercialRegistryNumber;
        }

        var subtitleParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(nationalMasked))
        {
            subtitleParts.Add(nationalMasked);
        }

        if (!string.IsNullOrWhiteSpace(registry))
        {
            subtitleParts.Add(registry);
        }

        subtitleParts.Add(primary.Msisdn);
        if (distinctLines.Count > 1)
        {
            subtitleParts.Add($"{distinctLines.Count} خطوط");
        }

        results.Add(new TelecomUniversalSearchRowDto
        {
            ResultType = "Customer",
            Id = customerId,
            CustomerId = customerId,
            SubscriberProfileId = primary.ProfileId,
            CustomerNameAr = displayName ?? profiles.FirstOrDefault()?.Customer?.DisplayName,
            NationalId = nationalMasked,
            Msisdn = primary.Msisdn,
            Status = status.ToString(),
            Title = displayName ?? profiles.FirstOrDefault()?.Customer?.DisplayName,
            Subtitle = string.Join(" · ", subtitleParts),
        });
    }

    private static TelecomUniversalSearchRowDto BuildCustomerOnlyRow(
        string customerId,
        string? displayName,
        CustomerStatus status,
        string? nationalId,
        string? msisdn)
    {
        var masked = MaskNationalId(nationalId);
        return new TelecomUniversalSearchRowDto
        {
            ResultType = "Customer",
            Id = customerId,
            CustomerId = customerId,
            CustomerNameAr = displayName,
            NationalId = masked,
            Msisdn = msisdn,
            Status = status.ToString(),
            Title = displayName,
            Subtitle = string.Join(" · ", new[] { masked, msisdn }.Where(s => !string.IsNullOrWhiteSpace(s))),
        };
    }

    private static void AppendProfileRows(
        List<TelecomUniversalSearchRowDto> results,
        List<SubscriberProfile> profiles,
        string? nationalIdDigits,
        string? commercialRegistry = null,
        string? forcedMsisdn = null)
    {
        foreach (var p in profiles)
        {
            if (results.Any(r => r.SubscriberProfileId == p.Id))
            {
                continue;
            }

            var activeSubs = p.Subscriptions.Where(s => !s.IsDeleted).ToList();
            var primarySub = GetCustomer360Handler.DeduplicateSubscriptionsByLine(activeSubs)
                .OrderByDescending(s => s.IsPrimaryLine)
                .ThenByDescending(s => s.CreatedAtUtc ?? DateTime.MinValue)
                .FirstOrDefault();
            var msisdn = forcedMsisdn ?? primarySub?.MsisdnAsset?.Msisdn;

            if (!string.IsNullOrWhiteSpace(msisdn))
            {
                var canonicalProfileId = primarySub?.MsisdnAsset?.SubscriberProfileId;
                var duplicate = results.Find(r =>
                    string.Equals(r.Msisdn, msisdn, StringComparison.Ordinal));

                if (duplicate != null)
                {
                    if (canonicalProfileId == p.Id && duplicate.SubscriberProfileId != p.Id)
                    {
                        results.Remove(duplicate);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            var typeLabel = primarySub?.SubscriptionTypeLookup?.NameAr
                            ?? primarySub?.SubscriptionTypeLookup?.NameEn
                            ?? primarySub?.SubscriptionTypeLookup?.Code;

            string? nationalMasked = null;
            string? registry = commercialRegistry;
            if (p.Customer is IndividualCustomer ind)
            {
                nationalMasked = MaskNationalId(ind.NationalId);
            }
            else if (p.Customer is CorporateCustomer corp)
            {
                registry ??= corp.CommercialRegistryNumber;
            }

            var subtitleParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(nationalMasked))
            {
                subtitleParts.Add(nationalMasked);
            }

            if (!string.IsNullOrWhiteSpace(registry))
            {
                subtitleParts.Add(registry);
            }

            if (!string.IsNullOrWhiteSpace(msisdn))
            {
                subtitleParts.Add(msisdn);
            }

            if (!string.IsNullOrWhiteSpace(typeLabel))
            {
                subtitleParts.Add(typeLabel);
            }

            var status = p.Customer?.Status.ToString() ?? p.OperationalStatus.ToString();
            var displayName = p.Customer?.DisplayName;

            results.Add(new TelecomUniversalSearchRowDto
            {
                ResultType = "SubscriberProfile",
                Id = p.Id,
                SubscriberProfileId = p.Id,
                CustomerId = p.CustomerId,
                CustomerNameAr = displayName,
                NationalId = nationalMasked ?? (nationalIdDigits != null ? MaskNationalId(nationalIdDigits) : null),
                Msisdn = msisdn,
                Status = status,
                Title = displayName,
                Subtitle = subtitleParts.Count > 0 ? string.Join(" · ", subtitleParts) : null,
            });
        }
    }

    /// <summary>One hit per customer (identity) or per MSISDN (line lookup) — drops orphan rows without a number.</summary>
    private static List<TelecomUniversalSearchRowDto> DeduplicateSearchRows(IReadOnlyList<TelecomUniversalSearchRowDto> rows)
    {
        if (rows.Count <= 1)
        {
            return rows.ToList();
        }

        var byCustomer = new Dictionary<string, TelecomUniversalSearchRowDto>(StringComparer.Ordinal);
        var lineOnly = new List<TelecomUniversalSearchRowDto>();

        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.CustomerId))
            {
                if (!string.IsNullOrWhiteSpace(row.Msisdn))
                {
                    lineOnly.Add(row);
                }

                continue;
            }

            if (!byCustomer.TryGetValue(row.CustomerId, out var existing) || PreferCustomerSearchRow(row, existing))
            {
                byCustomer[row.CustomerId] = row;
            }
        }

        var merged = lineOnly.Concat(byCustomer.Values).ToList();
        var kept = new List<TelecomUniversalSearchRowDto>();
        foreach (var row in merged)
        {
            var msisdn = row.Msisdn?.Trim();
            if (string.IsNullOrEmpty(msisdn))
            {
                continue;
            }

            var existing = kept.Find(r => string.Equals(r.Msisdn, msisdn, StringComparison.Ordinal));
            if (existing != null)
            {
                if (PreferSearchRow(row, existing))
                {
                    kept.Remove(existing);
                    kept.Add(row);
                }

                continue;
            }

            kept.Add(row);
        }

        return kept;
    }

    private static bool PreferCustomerSearchRow(
        TelecomUniversalSearchRowDto candidate,
        TelecomUniversalSearchRowDto incumbent)
    {
        var candidateHasMsisdn = !string.IsNullOrWhiteSpace(candidate.Msisdn);
        var incumbentHasMsisdn = !string.IsNullOrWhiteSpace(incumbent.Msisdn);
        if (candidateHasMsisdn != incumbentHasMsisdn)
        {
            return candidateHasMsisdn;
        }

        return PreferSearchRow(candidate, incumbent);
    }

    private static bool PreferSearchRow(TelecomUniversalSearchRowDto candidate, TelecomUniversalSearchRowDto incumbent)
    {
        var candidateHasProfile = !string.IsNullOrEmpty(candidate.SubscriberProfileId);
        var incumbentHasProfile = !string.IsNullOrEmpty(incumbent.SubscriberProfileId);
        if (candidateHasProfile != incumbentHasProfile)
        {
            return candidateHasProfile;
        }

        var candidateHasCustomer = !string.IsNullOrEmpty(candidate.CustomerId);
        var incumbentHasCustomer = !string.IsNullOrEmpty(incumbent.CustomerId);
        if (candidateHasCustomer != incumbentHasCustomer)
        {
            return candidateHasCustomer;
        }

        return string.Compare(candidate.SubscriberProfileId, incumbent.SubscriberProfileId, StringComparison.Ordinal)
               > 0;
    }
}
