using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.DeviceSales;

public sealed class DeviceSaleCompletionService : IDeviceSaleCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<DeviceInventory> _deviceRepository;
    private readonly ICommandRepository<DeviceInstallmentContract> _contractRepository;
    private readonly ICommandRepository<DeviceInstallmentScheduleLine> _scheduleRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IBillingSystemIntegration _billing;
    private readonly IDeviceInventoryIntegration _deviceInventoryIntegration;
    private readonly ISmsGatewayIntegration _sms;
    private readonly NumberSequenceService _numberSequence;
    private readonly IUnitOfWork _unitOfWork;

    public DeviceSaleCompletionService(
        IQueryContext query,
        ICommandRepository<DeviceInventory> deviceRepository,
        ICommandRepository<DeviceInstallmentContract> contractRepository,
        ICommandRepository<DeviceInstallmentScheduleLine> scheduleRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IBillingSystemIntegration billing,
        IDeviceInventoryIntegration deviceInventoryIntegration,
        ISmsGatewayIntegration sms,
        NumberSequenceService numberSequence,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _deviceRepository = deviceRepository;
        _contractRepository = contractRepository;
        _scheduleRepository = scheduleRepository;
        _operationRepository = operationRepository;
        _billing = billing;
        _deviceInventoryIntegration = deviceInventoryIntegration;
        _sms = sms;
        _numberSequence = numberSequence;
        _unitOfWork = unitOfWork;
    }

    public async Task FulfillAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.DeviceSale || string.IsNullOrEmpty(operation.DeviceInventoryId))
        {
            return;
        }

        var device = await _deviceRepository.GetAsync(operation.DeviceInventoryId, cancellationToken)
            ?? throw new InvalidOperationException("Device inventory not found.");

        var msisdn = await ResolveMsisdnAsync(operation, cancellationToken);

        var chargeResult = await _billing.ProvisionAsync(
            new BillingProvisionRequest(
                operation.Id,
                operation.Number,
                msisdn,
                TelecomOperationKind.DeviceSale,
                operation.CorrelationId,
                operation.DeviceDownPaymentAmount ?? device.ListPrice,
                ProductServiceCode: TelecomBssOperations.CbsPostDeviceSale),
            cancellationToken);

        if (!chargeResult.Success)
        {
            throw new InvalidOperationException(chargeResult.Message ?? "فشل ترحيل بيع الجهاز إلى CBS.");
        }

        DeviceInstallmentContract? contract = null;
        if (operation.DeviceSaleType == DeviceSaleType.Installment && !string.IsNullOrEmpty(operation.InstallmentPlanId))
        {
            var plan = await _query.InstallmentPlan.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == operation.InstallmentPlanId, cancellationToken);

            var contractNumber = _numberSequence.GenerateNumber("DeviceInstallmentContract", "DIC-", "", useDate: true);

            var cbsContract = await _billing.ProvisionAsync(
                new BillingProvisionRequest(
                    operation.Id,
                    contractNumber,
                    msisdn,
                    TelecomOperationKind.DeviceSale,
                    operation.CorrelationId,
                    operation.DeviceMonthlyInstallmentAmount ?? 0m,
                    ProductServiceCode: TelecomBssOperations.CbsCreateDeviceInstallmentContract),
                cancellationToken);

            if (!cbsContract.Success)
            {
                throw new InvalidOperationException(cbsContract.Message ?? "فشل إنشاء عقد التقسيط في CBS.");
            }

            contract = new DeviceInstallmentContract
            {
                TelecomOperationRequestId = operation.Id,
                ContractNumber = contractNumber,
                Status = InstallmentContractStatus.Active,
                DownPayment = operation.DeviceDownPaymentAmount ?? 0m,
                MonthlyAmount = operation.DeviceMonthlyInstallmentAmount ?? 0m,
                CreditScoreSnapshot = operation.DeviceCreditScoreSnapshot,
                CbsContractId = contractNumber,
                InstallmentPlanId = operation.InstallmentPlanId,
                WarrantyStartsAtUtc = DateTime.UtcNow,
                CreatedById = actorUserId,
            };
            await _contractRepository.CreateAsync(contract, cancellationToken);

            if (plan != null)
            {
                await GenerateScheduleAsync(contract, plan.Months, plan.Months > 0
                    ? (operation.DeviceMonthlyInstallmentAmount ?? 0m)
                    : 0m, actorUserId, cancellationToken);
            }

            operation.DeviceInstallmentContractId = contract.Id;
        }

        device.TransitionTo(DeviceInventoryStatus.Sold);
        _deviceRepository.Update(device);

        operation.DeviceWarrantyStartsAtUtc = DateTime.UtcNow;
        operation.ProvisioningResult = "DeviceSaleCompleted";
        _operationRepository.Update(operation);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(msisdn))
        {
            await _sms.SendAsync(
                msisdn,
                $"تم بيع الجهاز بنجاح — مرجع العملية {operation.Number}. الضمان ساري من اليوم.",
                cancellationToken);
        }
    }

    public async Task CompensateOnFailureAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(operation.DeviceInventoryId))
        {
            return;
        }

        var device = await _deviceRepository.GetAsync(operation.DeviceInventoryId, cancellationToken);
        if (device == null)
        {
            return;
        }

        if (device.Status == DeviceInventoryStatus.Reserved)
        {
            device.ReleaseReservation();
            _deviceRepository.Update(device);
        }

        await _deviceInventoryIntegration.ReleaseAsync(device.Imei, operation.Id, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task GenerateScheduleAsync(
        DeviceInstallmentContract contract,
        int months,
        decimal monthlyAmount,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow.Date.AddMonths(1);
        for (var i = 1; i <= months; i++)
        {
            await _scheduleRepository.CreateAsync(
                new DeviceInstallmentScheduleLine
                {
                    DeviceInstallmentContractId = contract.Id,
                    Sequence = i,
                    DueDateUtc = start.AddMonths(i - 1),
                    Amount = monthlyAmount,
                    Status = InstallmentScheduleLineStatus.Pending,
                    CreatedById = actorUserId,
                },
                cancellationToken);
        }
    }

    private async Task<string?> ResolveMsisdnAsync(TelecomOperationRequest operation, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            return null;
        }

        return await _query.MsisdnAsset.AsNoTracking()
            .Where(m => m.Id == operation.MsisdnAssetId)
            .Select(m => m.Msisdn)
            .FirstOrDefaultAsync(ct);
    }

}
