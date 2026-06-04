using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>VAL-14-04 — marks overdue installment lines and opens collections tickets.</summary>
public sealed class DeviceInstallmentDelinquencyHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeviceInstallmentDelinquencyHostedService> _logger;

    public DeviceInstallmentDelinquencyHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeviceInstallmentDelinquencyHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOverdueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device installment delinquency job failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessOverdueAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var contractRepo = scope.ServiceProvider.GetRequiredService<ICommandRepository<DeviceInstallmentContract>>();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<ICommandRepository<DeviceInstallmentScheduleLine>>();
        var ticketQueue = scope.ServiceProvider.GetRequiredService<ITechnicalTicketQueueIngestionService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var overdueLines = await query.DeviceInstallmentScheduleLine.AsNoTracking().IsDeletedEqualTo()
            .Where(l => l.Status == InstallmentScheduleLineStatus.Pending && l.DueDateUtc < now)
            .Select(l => new { l.Id, l.DeviceInstallmentContractId })
            .ToListAsync(cancellationToken);

        foreach (var line in overdueLines)
        {
            var sched = await scheduleRepo.GetAsync(line.Id, cancellationToken);
            if (sched == null) continue;
            sched.Status = InstallmentScheduleLineStatus.Overdue;
            scheduleRepo.Update(sched);

            var contract = await contractRepo.GetAsync(line.DeviceInstallmentContractId, cancellationToken);
            if (contract == null || contract.Status == InstallmentContractStatus.Delinquent)
            {
                continue;
            }

            contract.Status = InstallmentContractStatus.Delinquent;
            contract.DelinquencyStatus = "Overdue";
            contractRepo.Update(contract);

            var op = await query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                .FirstOrDefaultAsync(o => o.Id == contract.TelecomOperationRequestId, cancellationToken);
            if (op != null)
            {
                var operation = await query.TelecomOperationRequest
                    .FirstOrDefaultAsync(o => o.Id == op.Id, cancellationToken);
                if (operation != null)
                {
                    await ticketQueue.EnqueueDeviceInstallmentCollectionsAsync(
                        operation,
                        contract.ContractNumber,
                        "VAL-14-04: قسط متأخر — يتطلب متابعة تحصيل",
                        cancellationToken);
                }
            }
        }

        if (overdueLines.Count > 0)
        {
            await unitOfWork.SaveAsync(cancellationToken);
        }
    }
}
