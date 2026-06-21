using Application.Common.Telecom.Customer360;
using Application.Features.TelecomManager.Queries;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.UniversalSearch;

public static class TelecomUniversalSearchRowComposer
{
    /// <summary>Identity search: one row per customer with primary MSISDN — lines live on Customer 360.</summary>
    public static void AppendCustomerConsolidatedRow(
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
            var subs = Customer360SubscriptionDeduplicator.DeduplicateByLine(
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

        string? nationalMasked = nationalIdDigits != null ? TelecomUniversalSearchTerm.MaskNationalId(nationalIdDigits) : null;
        if (nationalMasked == null && profiles.FirstOrDefault()?.Customer is IndividualCustomer ind)
        {
            nationalMasked = TelecomUniversalSearchTerm.MaskNationalId(ind.NationalId);
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

    public static void AppendProfileRows(
        List<TelecomUniversalSearchRowDto> results,
        List<SubscriberProfile> profiles,
        string? nationalIdDigits,
        string? commercialRegistry = null,
        string? forcedMsisdn = null)
    {
        foreach (var p in profiles)
        {
            if (p.Customer == null)
            {
                continue;
            }

            if (results.Any(r => r.SubscriberProfileId == p.Id))
            {
                continue;
            }

            var activeSubs = p.Subscriptions.Where(s => !s.IsDeleted).ToList();
            var primarySub = Customer360SubscriptionDeduplicator.DeduplicateByLine(activeSubs)
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
                nationalMasked = TelecomUniversalSearchTerm.MaskNationalId(ind.NationalId);
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
                NationalId = nationalMasked ?? (nationalIdDigits != null ? TelecomUniversalSearchTerm.MaskNationalId(nationalIdDigits) : null),
                Msisdn = msisdn,
                Status = status,
                Title = displayName,
                Subtitle = subtitleParts.Count > 0 ? string.Join(" · ", subtitleParts) : null,
            });
        }
    }

    /// <summary>One hit per customer (identity) or per MSISDN (line lookup) — drops orphan rows without a number.</summary>
    public static List<TelecomUniversalSearchRowDto> DeduplicateSearchRows(IReadOnlyList<TelecomUniversalSearchRowDto> rows)
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

    private static TelecomUniversalSearchRowDto BuildCustomerOnlyRow(
        string customerId,
        string? displayName,
        CustomerStatus status,
        string? nationalId,
        string? msisdn)
    {
        var masked = TelecomUniversalSearchTerm.MaskNationalId(nationalId);
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
