using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Telecom.OfferSubscription;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public class GetMigrationProrationPreviewResult
{
    public string? CurrentPlanName { get; init; }
    public string? TargetPlanName { get; init; }
    public decimal CurrentMonthlyPrice { get; init; }
    public decimal TargetMonthlyPrice { get; init; }
    public decimal PriceDifference { get; init; }
    public decimal ProratedAmount { get; init; }
    public decimal WalletBalance { get; init; }
    public bool SufficientBalance { get; init; }
    public int DaysRemainingInCycle { get; init; }
    public int DaysInBillingCycle { get; init; }
    public string CurrencyCode { get; init; } = "SYP";
}

public class GetMigrationProrationPreviewRequest : IRequest<GetMigrationProrationPreviewResult>, IRequireAnyPermission
{
    public string SubscriberProfileId { get; init; } = "";
    public string MsisdnAssetId { get; init; } = "";
    public string ProductOfferingId { get; init; } = "";
    public IReadOnlyList<string> PermissionKeys => ProductCatalogPermissionSets.ReadAny;
}

public class GetMigrationProrationPreviewHandler
    : IRequestHandler<GetMigrationProrationPreviewRequest, GetMigrationProrationPreviewResult>
{
    private readonly IQueryContext _context;
    private readonly IBillingSystemIntegration _billing;

    public GetMigrationProrationPreviewHandler(IQueryContext context, IBillingSystemIntegration billing)
    {
        _context = context;
        _billing = billing;
    }

    public async Task<GetMigrationProrationPreviewResult> Handle(
        GetMigrationProrationPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = (request.SubscriberProfileId ?? string.Empty).Trim();
        var assetId = (request.MsisdnAssetId ?? string.Empty).Trim();
        var offeringId = (request.ProductOfferingId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(assetId) || string.IsNullOrEmpty(offeringId))
        {
            throw new BusinessRuleViolationException("معاينة الترحيل تتطلب ملف المشترك والخط والعرض الجديد.");
        }

        var subscription = await _context.TelecomSubscription.AsNoTracking().IsDeletedEqualTo(false)
            .Include(s => s.Product)
            .FirstOrDefaultAsync(
                s => s.SubscriberProfileId == profileId && s.MsisdnAssetId == assetId,
                cancellationToken)
            ?? throw new BusinessRuleViolationException("لا يوجد اشتراك على هذا الخط.");

        var targetOffering = await _context.ProductOffering.AsNoTracking().IsDeletedEqualTo(false)
            .FirstOrDefaultAsync(o => o.Id == offeringId, cancellationToken)
            ?? throw new BusinessRuleViolationException("العرض التجاري المختار غير موجود.");

        var targetProductId = (targetOffering.ProductId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(targetProductId))
        {
            throw new BusinessRuleViolationException("العرض المختار غير مربوط بمنتج تقني.");
        }

        var targetProduct = await _context.Product.AsNoTracking().IsDeletedEqualTo(false)
            .FirstOrDefaultAsync(p => p.Id == targetProductId, cancellationToken)
            ?? throw new BusinessRuleViolationException("المنتج التقني للعرض الجديد غير موجود.");

        Product? currentProduct = subscription.Product;
        if (currentProduct == null && !string.IsNullOrEmpty(subscription.ProductId))
        {
            currentProduct = await _context.Product.AsNoTracking().IsDeletedEqualTo(false)
                .FirstOrDefaultAsync(p => p.Id == subscription.ProductId, cancellationToken);
        }

        var currentPrice = (decimal)(currentProduct?.UnitPrice ?? 0d);
        var targetPrice = (decimal)(targetProduct.UnitPrice ?? 0d);
        var priceDifference = targetPrice - currentPrice;

        var now = DateTime.UtcNow;
        var (daysRemaining, daysInCycle) = MigrationProrationCalculator.GetBillingCycleFraction(now);
        var prorated = MigrationProrationCalculator.CalculateProratedAmount(priceDifference, daysRemaining, daysInCycle);

        var profile = await _context.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);
        var walletBalance = profile?.PrepaidBalance ?? 0m;

        var asset = await _context.MsisdnAsset.AsNoTracking().IsDeletedEqualTo(false)
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken);
        if (!string.IsNullOrEmpty(asset?.Msisdn))
        {
            try
            {
                var outstanding = await _billing.GetOutstandingBalanceAsync(asset.Msisdn, cancellationToken);
                if (outstanding < 0)
                {
                    walletBalance = 0m;
                }
            }
            catch
            {
                /* keep prepaid snapshot */
            }
        }

        var sufficient = prorated <= 0 || walletBalance >= prorated;

        string? currentPlanName = currentProduct?.Name;
        if (string.IsNullOrEmpty(currentPlanName) && !string.IsNullOrEmpty(subscription.ProductOfferingId))
        {
            currentPlanName = await _context.ProductOffering.AsNoTracking().IsDeletedEqualTo(false)
                .Where(o => o.Id == subscription.ProductOfferingId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new GetMigrationProrationPreviewResult
        {
            CurrentPlanName = currentPlanName,
            TargetPlanName = targetOffering.Name,
            CurrentMonthlyPrice = currentPrice,
            TargetMonthlyPrice = targetPrice,
            PriceDifference = priceDifference,
            ProratedAmount = prorated,
            WalletBalance = walletBalance,
            SufficientBalance = sufficient,
            DaysRemainingInCycle = daysRemaining,
            DaysInBillingCycle = daysInCycle,
        };
    }
}
