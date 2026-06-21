using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Features.TelecomManager.Queries;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.UniversalSearch;

public interface ITelecomUniversalSearchDataService
{
    Task SearchByNationalIdAsync(
        List<TelecomUniversalSearchRowDto> results,
        string nationalIdDigits,
        int maxRows,
        CancellationToken cancellationToken);

    Task SearchByCommercialRegistryAsync(
        List<TelecomUniversalSearchRowDto> results,
        string registryTerm,
        int maxRows,
        CancellationToken cancellationToken);

    Task SearchByMsisdnAsync(
        List<TelecomUniversalSearchRowDto> results,
        string canonicalMsisdn,
        bool availableOnly,
        int maxRows,
        CancellationToken cancellationToken);
}

public sealed class TelecomUniversalSearchDataService : ITelecomUniversalSearchDataService
{
    private readonly IQueryContext _context;
    private readonly IFieldEncryptionService _encryption;

    public TelecomUniversalSearchDataService(IQueryContext context, IFieldEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task SearchByNationalIdAsync(
        List<TelecomUniversalSearchRowDto> results,
        string nationalIdDigits,
        int maxRows,
        CancellationToken cancellationToken)
    {
        var hash = _encryption.ComputeSearchHash(nationalIdDigits);
        var customers = await _context.Customer
            .AsNoTracking()
            .IsDeletedEqualTo()
            .OfType<IndividualCustomer>()
            .Where(c => c.NationalIdSearchHash == hash)
            .Select(c => new { c.Id, c.DisplayName, c.Status })
            .Take(maxRows)
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
            TelecomUniversalSearchRowComposer.AppendCustomerConsolidatedRow(
                results,
                customer.Id,
                customer.DisplayName,
                customer.Status,
                customerProfiles,
                nationalIdDigits);
        }
    }

    public async Task SearchByCommercialRegistryAsync(
        List<TelecomUniversalSearchRowDto> results,
        string registryTerm,
        int maxRows,
        CancellationToken cancellationToken)
    {
        var normalizedRegistry = registryTerm.Trim();
        var customers = await _context.Customer
            .AsNoTracking()
            .IsDeletedEqualTo()
            .OfType<CorporateCustomer>()
            .Where(c => c.CommercialRegistryNumber == normalizedRegistry)
            .Select(c => new { c.Id, c.DisplayName, c.Status })
            .Take(maxRows)
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
            TelecomUniversalSearchRowComposer.AppendCustomerConsolidatedRow(
                results,
                customer.Id,
                customer.DisplayName,
                customer.Status,
                customerProfiles,
                commercialRegistry: normalizedRegistry);
        }
    }

    public async Task SearchByMsisdnAsync(
        List<TelecomUniversalSearchRowDto> results,
        string canonicalMsisdn,
        bool availableOnly,
        int maxRows,
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

        var assets = await query.Take(maxRows).ToListAsync(cancellationToken);

        foreach (var m in assets)
        {
            if (m.SubscriberProfileId != null && results.Any(r => r.SubscriberProfileId == m.SubscriberProfileId))
            {
                continue;
            }

            if (m.SubscriberProfile != null)
            {
                if (m.SubscriberProfile.Customer == null)
                {
                    continue;
                }

                TelecomUniversalSearchRowComposer.AppendProfileRows(results, [m.SubscriberProfile], null, null, m.Msisdn);
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
}
