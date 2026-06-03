using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.DeviceSales;

public sealed class DeviceSalesEligibilityChecker : IDeviceSalesEligibilityChecker
{
    private static readonly TelecomOperationStatus[] BlockingStatuses =
    [
        TelecomOperationStatus.Draft,
        TelecomOperationStatus.PendingDocuments,
        TelecomOperationStatus.Confirmed,
        TelecomOperationStatus.Provisioning,
        TelecomOperationStatus.PendingExternal
    ];

    private readonly IQueryContext _query;

    public DeviceSalesEligibilityChecker(IQueryContext query) => _query = query;

    public async Task<DeviceSalesEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string deviceInventoryId,
        DeviceSaleType saleType,
        string? installmentPlanId,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        var profileId = subscriberProfileId.Trim();
        var deviceId = deviceInventoryId.Trim();

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        if (profile.Customer?.Status == CustomerStatus.Blacklisted)
        {
            return Deny("VAL-14-02: العميل محظور — لا يمكن بيع جهاز.");
        }

        var device = await _query.DeviceInventory.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken)
            ?? throw new BusinessRuleViolationException("الجهاز غير موجود في المخزون.");

        if (device.Status != DeviceInventoryStatus.Available)
        {
            return Deny("VAL-14-01: IMEI غير متاح (محجوز أو مباع).");
        }

        var imeiDup = await _query.DeviceInventory.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(d => d.Imei == device.Imei && d.Id != device.Id, cancellationToken);
        if (imeiDup)
        {
            return Deny("VAL-14-01: IMEI مكرر في المخزون.");
        }

        var blockingOp = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(o => o.Kind == TelecomOperationKind.DeviceSale
                           && o.DeviceInventoryId == deviceId
                           && BlockingStatuses.Contains(o.Status)
                           && (excludeOperationId == null || o.Id != excludeOperationId),
                cancellationToken);
        if (blockingOp)
        {
            return Deny("يوجد طلب بيع جهاز مفتوح لهذا IMEI.");
        }

        InstallmentPlan? plan = null;
        if (saleType == DeviceSaleType.Installment)
        {
            if (string.IsNullOrWhiteSpace(installmentPlanId))
            {
                return Deny("خطة التقسيط مطلوبة لبيع بالتقسيط.");
            }

            plan = await _query.InstallmentPlan.AsNoTracking().IsDeletedEqualTo()
                .FirstOrDefaultAsync(p => p.Id == installmentPlanId.Trim() && p.IsActive, cancellationToken)
                ?? throw new BusinessRuleViolationException("خطة التقسيط غير موجودة أو غير فعّالة.");
        }

        var creditScore = ResolveCreditScore(profile.CustomerId);
        var hasDelinquent = await HasDelinquentContractAsync(profile.CustomerId, cancellationToken);
        var isNew = await IsNewCustomerAsync(profile.CustomerId, cancellationToken);
        var isVip = false;

        DeviceFinancingMatrixResult? matrix = null;
        if (saleType == DeviceSaleType.Installment && plan != null)
        {
            matrix = DeviceFinancingDecisionMatrix.Evaluate(new DeviceFinancingMatrixInput(
                creditScore, hasDelinquent, isNew, isVip));

            if (matrix.Decision == DeviceFinancingDecision.Rejected)
            {
                return Deny(matrix.NoteAr);
            }

            if (creditScore < plan.MinCreditScore && matrix.Decision != DeviceFinancingDecision.Conditional)
            {
                return Deny($"VAL-14-02: درجة الائتمان ({creditScore}) أقل من الحد الأدنى للخطة ({plan.MinCreditScore}).");
            }
        }

        decimal? downPayment = null;
        decimal? monthly = null;
        if (plan != null && matrix != null)
        {
            var financed = device.ListPrice * (1m - matrix.RequiredDownPaymentPercent / 100m);
            monthly = Math.Round(financed / plan.Months, 2);
            downPayment = Math.Round(device.ListPrice * matrix.RequiredDownPaymentPercent / 100m, 2);
        }
        else if (saleType == DeviceSaleType.Cash)
        {
            downPayment = device.ListPrice;
        }

        return new DeviceSalesEligibilityResult(
            true,
            matrix?.NoteAr ?? "أهلية بيع جهاز — مقبول.",
            device.Id,
            matrix?.Decision,
            matrix?.ApprovalLevelRequired,
            matrix?.NoteAr,
            downPayment,
            monthly,
            creditScore,
            saleType == DeviceSaleType.Installment
            && matrix?.ApprovalLevelRequired is DeviceSaleWellKnown.ApprovalFinanceOfficer
                or DeviceSaleWellKnown.ApprovalManager
                or DeviceSaleWellKnown.ApprovalSupervisor);
    }

    public async Task<DeviceSalesEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.DeviceSale)
        {
            return new DeviceSalesEligibilityResult(false, "نوع العملية ليس بيع جهاز.");
        }

        if (string.IsNullOrEmpty(operation.DeviceInventoryId))
        {
            return Deny("الجهاز غير محدد.");
        }

        if (operation.DeviceSaleType == DeviceSaleType.Installment)
        {
            if (operation.DeviceFinancingDecision == DeviceFinancingDecision.Rejected)
            {
                return Deny("قرار التمويل مرفوض.");
            }

            if (operation.DeviceFinancingDecision != DeviceFinancingDecision.Approved
                && operation.DeviceFinancingDecision != DeviceFinancingDecision.Conditional
                && operation.DeviceFinancingDecision != DeviceFinancingDecision.DepositRequired)
            {
                return Deny("يلزم اعتماد التمويل قبل التأكيد.");
            }

            if (!string.IsNullOrEmpty(operation.DeviceApprovalLevelRequired)
                && operation.DeviceFinancingDecision != DeviceFinancingDecision.Approved)
            {
                var approved = operation.Notes?.Contains("[FinanceApproved]", StringComparison.Ordinal) == true;
                if (!approved)
                {
                    return Deny("في انتظار اعتماد الفايننس/المشرف.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(operation.PaymentReference))
        {
            return Deny("VAL-14-03: الدفعة الأولى / مرجع الدفع مطلوب قبل التسليم.");
        }

        var device = await _query.DeviceInventory.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(d => d.Id == operation.DeviceInventoryId, cancellationToken);
        if (device == null || device.Status is not (DeviceInventoryStatus.Reserved or DeviceInventoryStatus.Available))
        {
            return Deny("VAL-14-01: حالة IMEI لا تسمح بالتسليم.");
        }

        return new DeviceSalesEligibilityResult(true, "جاهز للتأكيد.");
    }

    private async Task<bool> HasDelinquentContractAsync(string customerId, CancellationToken ct)
    {
        return await (
            from c in _query.DeviceInstallmentContract.AsNoTracking().IsDeletedEqualTo()
            join o in _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                on c.TelecomOperationRequestId equals o.Id
            join p in _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                on o.SubscriberProfileId equals p.Id
            where p.CustomerId == customerId
                  && c.Status == InstallmentContractStatus.Delinquent
            select c.Id).AnyAsync(ct);
    }

    private async Task<bool> IsNewCustomerAsync(string customerId, CancellationToken ct)
    {
        var completedSales = await (
            from o in _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            join p in _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                on o.SubscriberProfileId equals p.Id
            where p.CustomerId == customerId
                  && o.Kind == TelecomOperationKind.DeviceSale
                  && o.Status == TelecomOperationStatus.Completed
            select o.Id).CountAsync(ct);
        return completedSales == 0;
    }

    private static int ResolveCreditScore(string customerId)
    {
        var hash = Math.Abs(customerId.GetHashCode(StringComparison.Ordinal));
        return 450 + (hash % 350);
    }

    private static DeviceSalesEligibilityResult Deny(string message) =>
        new(false, message);
}
