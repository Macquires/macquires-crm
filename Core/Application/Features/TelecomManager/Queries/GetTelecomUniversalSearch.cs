using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
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

        var finalRows = results.Take(MaxRows).ToList();

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
        var profiles = await LoadSubscriberProfilesAsync(
            p => customerIds.Contains(p.CustomerId),
            cancellationToken);

        foreach (var customer in customers)
        {
            var customerProfiles = profiles.Where(p => p.CustomerId == customer.Id).ToList();
            if (customerProfiles.Count == 0)
            {
                results.Add(BuildCustomerOnlyRow(customer.Id, customer.DisplayName, customer.Status, nationalIdDigits, null));
                continue;
            }

            AppendProfileRows(results, customerProfiles, nationalIdDigits);
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

        AppendProfileRows(results, profiles, null, normalizedRegistry);
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
            .Where(x => !x.IsDeleted)
            .Where(predicate)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);
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

            var primarySub = p.Subscriptions
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.IsPrimaryLine)
                .FirstOrDefault();
            var msisdn = forcedMsisdn ?? primarySub?.MsisdnAsset?.Msisdn;
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
}
