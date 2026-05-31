using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.VasManager.Queries;

public record SubscriberVasPanelItemDto
{
    public string ServiceId { get; init; } = "";
    public string ServiceCode { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public decimal MonthlyFee { get; init; }
    public bool IsActiveOnLine { get; init; }
    public string? SubscriberActiveServiceId { get; init; }
}

public class GetSubscriberVasPanelResult
{
    public string Msisdn { get; init; } = "";
    public string TelecomSubscriptionId { get; init; } = "";
    public List<SubscriberVasPanelItemDto> Services { get; init; } = new();
}

public class GetSubscriberVasPanelRequest : IRequest<GetSubscriberVasPanelResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.TelecomVasToggle;
    public string? Msisdn { get; init; }
    public string? TelecomSubscriptionId { get; init; }
}

public class GetSubscriberVasPanelHandler : IRequestHandler<GetSubscriberVasPanelRequest, GetSubscriberVasPanelResult>
{
    private readonly IQueryContext _context;

    public GetSubscriberVasPanelHandler(IQueryContext context) => _context = context;

    public async Task<GetSubscriberVasPanelResult> Handle(
        GetSubscriberVasPanelRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await ResolveSubscriptionAsync(request, cancellationToken)
            ?? throw new InvalidOperationException("No subscription found for the given line.");

        var msisdn = subscription.MsisdnAsset?.Msisdn
            ?? throw new InvalidOperationException("Subscription has no MSISDN.");

        var catalog = await _context.TelecomValueAddedService
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ServiceCode)
            .ToListAsync(cancellationToken);

        var activeRows = await _context.SubscriberActiveService
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.TelecomSubscriptionId == subscription.Id
                && x.Status == SubscriberVasStatus.Active)
            .ToListAsync(cancellationToken);

        var activeByServiceId = activeRows.ToDictionary(x => x.TelecomValueAddedServiceId, x => x);

        var services = catalog.Select(v =>
        {
            activeByServiceId.TryGetValue(v.Id, out var row);
            return new SubscriberVasPanelItemDto
            {
                ServiceId = v.Id,
                ServiceCode = v.ServiceCode,
                NameAr = v.NameAr,
                NameEn = v.NameEn,
                MonthlyFee = v.MonthlyFee,
                IsActiveOnLine = row != null,
                SubscriberActiveServiceId = row?.Id,
            };
        }).ToList();

        return new GetSubscriberVasPanelResult
        {
            Msisdn = msisdn,
            TelecomSubscriptionId = subscription.Id,
            Services = services,
        };
    }

    private async Task<Domain.Entities.TelecomSubscription?> ResolveSubscriptionAsync(
        GetSubscriberVasPanelRequest request,
        CancellationToken cancellationToken)
    {
        var subId = (request.TelecomSubscriptionId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(subId))
        {
            return await _context.TelecomSubscription
                .AsNoTracking()
                .Include(s => s.MsisdnAsset)
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == subId, cancellationToken);
        }

        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.Msisdn ?? string.Empty);
        if (msisdn == null)
        {
            return null;
        }

        return await _context.TelecomSubscription
            .AsNoTracking()
            .Include(s => s.MsisdnAsset)
            .FirstOrDefaultAsync(
                s => !s.IsDeleted && s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == msisdn,
                cancellationToken);
    }
}
