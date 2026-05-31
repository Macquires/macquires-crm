using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Audit;

public class UserAuditReadService : IUserAuditReadService
{
    private readonly DataContext _context;
    private readonly IFieldEncryptionService _encryption;

    public UserAuditReadService(DataContext context, IFieldEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<int> CountAsync(UserAuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var q = await ApplyFiltersAsync(query, cancellationToken);
        return await q.CountAsync(cancellationToken);
    }

    public async Task<UserAuditLogQueryResult> QueryAsync(UserAuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(query.Take, 1, 500);
        var skip = Math.Max(0, query.Skip);

        var q = await ApplyFiltersAsync(query, cancellationToken);

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var userIds = rows
            .SelectMany(x => new[] { x.ActorUserId, x.UserId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var names = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Display = (u.FirstName ?? "") + " " + (u.LastName ?? "") })
            .ToDictionaryAsync(x => x.Id, x => x.Display.Trim(), cancellationToken);

        var customerEntityIds = rows
            .Where(x => x.EntityType == nameof(Customer) && !string.IsNullOrEmpty(x.EntityId))
            .Select(x => x.EntityId!)
            .Distinct()
            .ToList();

        var customerNames = customerEntityIds.Count == 0
            ? new Dictionary<string, string>()
            : await _context.Customer
                .AsNoTracking()
                .Where(c => customerEntityIds.Contains(c.Id))
                .Select(c => new { c.Id, c.DisplayName })
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName ?? x.Id, cancellationToken);

        var items = rows.Select(x =>
        {
            string? target = null;
            if (x.EntityType == nameof(Customer) && x.EntityId != null && customerNames.TryGetValue(x.EntityId, out var cn))
            {
                target = cn;
            }
            else if (x.UserId != null && names.TryGetValue(x.UserId, out var targetUser))
            {
                target = targetUser;
            }
            else if (!string.IsNullOrEmpty(x.EntityId) && x.EntityType is nameof(TelecomOperationRequest) or "VAS")
            {
                target = x.EntityType;
            }
            else
            {
                target = x.UserId;
            }

            return new UserAuditLogListItemDto
            {
                Id = x.Id,
                UserId = x.UserId,
                ActorUserId = x.ActorUserId,
                ActionType = x.ActionType,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                SummaryAr = x.SummaryAr,
                PayloadJson = x.PayloadJson,
                OccurredAtUtc = x.OccurredAtUtc,
                IpAddress = x.IpAddress,
                ActorDisplayName = names.TryGetValue(x.ActorUserId, out var actor) ? actor : x.ActorUserId,
                TargetDisplayName = target,
            };
        }).ToList();

        return new UserAuditLogQueryResult { Items = items, TotalCount = total };
    }

    private async Task<IQueryable<UserAuditLog>> ApplyFiltersAsync(
        UserAuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var q = _context.UserAuditLog.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            q = q.Where(x => x.UserId == query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.ActorUserId))
        {
            q = q.Where(x => x.ActorUserId == query.ActorUserId);
        }

        if (query.ActionTypes is { Count: > 0 })
        {
            q = q.Where(x => query.ActionTypes.Contains(x.ActionType));
        }
        else if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            q = q.Where(x => x.ActionType == query.ActionType);
        }

        if (query.FromUtc.HasValue)
        {
            q = q.Where(x => x.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            q = q.Where(x => x.OccurredAtUtc <= query.ToUtc.Value);
        }

        var customerId = (query.CustomerId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(customerId))
        {
            q = await ApplyCustomerIdFilterAsync(q, customerId, cancellationToken);
        }
        else
        {
            var searchTerm = (query.SearchTerm ?? string.Empty).Trim();
            if (searchTerm.Length >= 2)
            {
                q = await ApplySubscriberSearchAsync(q, searchTerm, cancellationToken);
            }
        }

        return q;
    }

    private async Task<IQueryable<UserAuditLog>> ApplyCustomerIdFilterAsync(
        IQueryable<UserAuditLog> q,
        string customerId,
        CancellationToken cancellationToken)
    {
        var customerIds = new HashSet<string>(StringComparer.Ordinal) { customerId };
        var operationIds = await ResolveTelecomOperationIdsForSearchAsync(string.Empty, customerIds, cancellationToken);

        var msisdns = await _context.TelecomSubscription
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.SubscriberProfile.CustomerId == customerId && s.MsisdnAsset != null)
            .Select(s => s.MsisdnAsset!.Msisdn)
            .Where(m => m != null && m != "")
            .Distinct()
            .Take(20)
            .ToListAsync(cancellationToken);

        IQueryable<string>? matchingIds = q.Where(x =>
                x.EntityType == nameof(Customer) && x.EntityId == customerId)
            .Select(x => x.Id);

        if (operationIds.Count > 0)
        {
            var opMatch = q.Where(x =>
                    x.EntityType == nameof(TelecomOperationRequest)
                    && x.EntityId != null
                    && operationIds.Contains(x.EntityId))
                .Select(x => x.Id);
            matchingIds = matchingIds.Union(opMatch);
        }

        foreach (var msisdn in msisdns)
        {
            if (string.IsNullOrWhiteSpace(msisdn))
            {
                continue;
            }

            var m = msisdn;
            var textMatch = q.Where(x =>
                    (x.SummaryAr != null && x.SummaryAr.Contains(m))
                    || (x.PayloadJson != null && x.PayloadJson.Contains(m)))
                .Select(x => x.Id);
            matchingIds = matchingIds.Union(textMatch);
        }

        return q.Where(x => matchingIds.Contains(x.Id));
    }

    private async Task<IQueryable<UserAuditLog>> ApplySubscriberSearchAsync(
        IQueryable<UserAuditLog> q,
        string searchTerm,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(searchTerm, out _))
        {
            return q.Where(x =>
                x.EntityId == searchTerm
                || x.UserId == searchTerm
                || x.ActorUserId == searchTerm);
        }

        var customerIds = await ResolveCustomerIdsForSearchAsync(searchTerm, cancellationToken);
        var operationIds = await ResolveTelecomOperationIdsForSearchAsync(searchTerm, customerIds, cancellationToken);
        var fragments = AuditLogSearchFragments.Build(searchTerm);

        if (customerIds.Count == 0 && operationIds.Count == 0 && fragments.Count == 0)
        {
            return q.Where(_ => false);
        }

        IQueryable<string>? matchingIds = null;

        if (customerIds.Count > 0)
        {
            var customerMatch = q.Where(x =>
                    x.EntityType == nameof(Customer)
                    && x.EntityId != null
                    && customerIds.Contains(x.EntityId))
                .Select(x => x.Id);
            matchingIds = customerMatch;
        }

        if (operationIds.Count > 0)
        {
            var opMatch = q.Where(x =>
                    x.EntityType == nameof(TelecomOperationRequest)
                    && x.EntityId != null
                    && operationIds.Contains(x.EntityId))
                .Select(x => x.Id);
            matchingIds = matchingIds == null ? opMatch : matchingIds.Union(opMatch);
        }

        foreach (var frag in fragments)
        {
            var f = frag;
            var textMatch = q.Where(x =>
                    (x.SummaryAr != null && x.SummaryAr.Contains(f))
                    || (x.PayloadJson != null && x.PayloadJson.Contains(f)))
                .Select(x => x.Id);
            matchingIds = matchingIds == null ? textMatch : matchingIds.Union(textMatch);
        }

        if (matchingIds == null)
        {
            return q.Where(_ => false);
        }

        return q.Where(x => matchingIds.Contains(x.Id));
    }

    private async Task<HashSet<string>> ResolveCustomerIdsForSearchAsync(
        string searchTerm,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var textTerm = TelecomPhoneNormalizer.NormalizeIndicDigitsToAscii(searchTerm).Trim();
        var fragments = AuditLogSearchFragments.Build(searchTerm);

        var byName = await _context.Customer
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.DisplayName.Contains(textTerm))
            .Select(c => c.Id)
            .Take(50)
            .ToListAsync(cancellationToken);
        foreach (var id in byName)
        {
            ids.Add(id);
        }

        foreach (var frag in fragments)
        {
            var f = frag;
            var byPhone = await _context.Customer
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.PrimaryPhone != null && c.PrimaryPhone.Contains(f))
                .Select(c => c.Id)
                .Take(50)
                .ToListAsync(cancellationToken);
            foreach (var id in byPhone)
            {
                ids.Add(id);
            }

            var byMsisdn = await _context.TelecomSubscription
                .AsNoTracking()
                .Where(s =>
                    !s.IsDeleted
                    && s.MsisdnAsset != null
                    && s.MsisdnAsset.Msisdn != null
                    && s.MsisdnAsset.Msisdn.Contains(f))
                .Select(s => s.SubscriberProfile.CustomerId)
                .Take(50)
                .ToListAsync(cancellationToken);
            foreach (var id in byMsisdn)
            {
                ids.Add(id);
            }
        }

        var digits = TelecomPhoneNormalizer.DigitsOnly(textTerm);
        if (digits.Length == 10)
        {
            var hash = _encryption.ComputeSearchHash(digits);
            var byNational = await _context.Customer
                .AsNoTracking()
                .OfType<IndividualCustomer>()
                .Where(c => !c.IsDeleted && c.NationalIdSearchHash == hash)
                .Select(c => c.Id)
                .Take(10)
                .ToListAsync(cancellationToken);
            foreach (var id in byNational)
            {
                ids.Add(id);
            }
        }

        if (textTerm.Length >= 3)
        {
            var byRegistry = await _context.Customer
                .AsNoTracking()
                .OfType<CorporateCustomer>()
                .Where(c => !c.IsDeleted && c.CommercialRegistryNumber != null && c.CommercialRegistryNumber.Contains(textTerm))
                .Select(c => c.Id)
                .Take(20)
                .ToListAsync(cancellationToken);
            foreach (var id in byRegistry)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private async Task<HashSet<string>> ResolveTelecomOperationIdsForSearchAsync(
        string searchTerm,
        HashSet<string> customerIds,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var fragments = AuditLogSearchFragments.Build(searchTerm);

        if (customerIds.Count > 0)
        {
            var customerIdList = customerIds.ToList();
            var profileIds = await _context.SubscriberProfile
                .AsNoTracking()
                .Where(sp => !sp.IsDeleted && customerIdList.Contains(sp.CustomerId))
                .Select(sp => sp.Id)
                .ToListAsync(cancellationToken);

            if (profileIds.Count > 0)
            {
                var byProfile = await _context.TelecomOperationRequest
                    .AsNoTracking()
                    .Where(o => !o.IsDeleted && profileIds.Contains(o.SubscriberProfileId))
                    .Select(o => o.Id)
                    .Take(100)
                    .ToListAsync(cancellationToken);
                foreach (var id in byProfile)
                {
                    ids.Add(id);
                }
            }
        }

        foreach (var frag in fragments)
        {
            var f = frag;
            var byMsisdn = await _context.TelecomOperationRequest
                .AsNoTracking()
                .Where(o =>
                    !o.IsDeleted
                    && o.MsisdnAsset != null
                    && o.MsisdnAsset.Msisdn != null
                    && o.MsisdnAsset.Msisdn.Contains(f))
                .Select(o => o.Id)
                .Take(50)
                .ToListAsync(cancellationToken);
            foreach (var id in byMsisdn)
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
