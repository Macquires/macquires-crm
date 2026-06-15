using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Idempotent demo seed for IN and HLR provisioning integration logs (40+ rows each).
/// </summary>
public sealed class TelecomInHlrProvisioningLogSeeder
{
    private const int MinDemoRowsPerChannel = 42;

    private readonly IQueryContext _query;
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TelecomInHlrProvisioningLogSeeder(
        IQueryContext query,
        ICommandRepository<BillingIntegrationLog> logRepository,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _logRepository = logRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task EnsureDemoLogsAsync()
    {
        await EnsureInLogsAsync();
        await EnsureHlrLogsAsync();
    }

    private async Task EnsureInLogsAsync()
    {
        var inTargets = ProvisioningIntegrationLogTargets.InTargets;
        var existing = await _query.BillingIntegrationLog.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(x => x.IntegrationTarget != null && inTargets.Contains(x.IntegrationTarget));
        if (existing >= MinDemoRowsPerChannel)
        {
            return;
        }

        var operations = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(30)
            .Select(x => new { x.Id, x.Number, x.BranchId })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var templates = BuildInMessageTemplates();
        var toCreate = MinDemoRowsPerChannel - existing;

        for (var i = 0; i < toCreate; i++)
        {
            var op = operations.Count > 0 ? operations[i % operations.Count] : null;
            var template = templates[i % templates.Length];
            var msisdn = $"0939{(100000 + i):D6}";
            var message = template.Replace("{MSISDN}", msisdn, StringComparison.Ordinal);

            await _logRepository.CreateAsync(new BillingIntegrationLog
            {
                TelecomOperationRequestId = op?.Id,
                AttemptNumber = (i % 3) + 1,
                Success = i % 17 != 0,
                Message = message,
                IntegrationTarget = ProvisioningIntegrationLogTargets.InMock,
                CorrelationId = Guid.CreateVersion7().ToString(),
                CreatedAtUtc = now.AddMinutes(-(i * 7 + 3)),
                BranchId = op?.BranchId,
            });
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task EnsureHlrLogsAsync()
    {
        var hlrTargets = ProvisioningIntegrationLogTargets.HlrTargets;
        var existing = await _query.BillingIntegrationLog.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(x => x.IntegrationTarget != null && hlrTargets.Contains(x.IntegrationTarget));
        if (existing >= MinDemoRowsPerChannel)
        {
            return;
        }

        var operations = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(30)
            .Select(x => new { x.Id, x.Number, x.BranchId })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var templates = BuildHlrMessageTemplates();
        var targets = new[]
        {
            ProvisioningIntegrationLogTargets.HlrMock,
            ProvisioningIntegrationLogTargets.HlrHssMock,
            ProvisioningIntegrationLogTargets.HlrVasMock,
        };
        var toCreate = MinDemoRowsPerChannel - existing;

        for (var i = 0; i < toCreate; i++)
        {
            var op = operations.Count > 0 ? operations[i % operations.Count] : null;
            var template = templates[i % templates.Length];
            var msisdn = $"0939{(200000 + i):D6}";
            var imsi = $"45204120000{(1712 + i):D4}";
            var iccid = $"89103000000{(1000171 + i):D7}";
            var message = template
                .Replace("{MSISDN}", msisdn, StringComparison.Ordinal)
                .Replace("{IMSI}", imsi, StringComparison.Ordinal)
                .Replace("{ICCID}", iccid, StringComparison.Ordinal);

            await _logRepository.CreateAsync(new BillingIntegrationLog
            {
                TelecomOperationRequestId = op?.Id,
                AttemptNumber = (i % 2) + 1,
                Success = i % 19 != 0,
                Message = message,
                IntegrationTarget = targets[i % targets.Length],
                CorrelationId = Guid.CreateVersion7().ToString(),
                CreatedAtUtc = now.AddMinutes(-(i * 5 + 1)),
                BranchId = op?.BranchId,
            });
        }

        await _unitOfWork.SaveAsync();
    }

    private static string[] BuildInMessageTemplates() =>
    [
        "CreateInProfile MSISDN={MSISDN} profile=PREPAID_STD (Success)",
        "ProvisionVoiceQuota MSISDN={MSISDN} amount=200 mins (Success)",
        "ProvisionDataBundle MSISDN={MSISDN} amount=3GB (Success)",
        "ProvisionDataBundle MSISDN={MSISDN} amount=10GB (Success)",
        "CreditPrepaidBalance MSISDN={MSISDN} amount=5000 SYP tx=TOPUP-DEMO (Success)",
        "UpdatePrepaidLifecycleState MSISDN={MSISDN} state=Active (Success)",
        "ProvisionVoiceQuota MSISDN={MSISDN} amount=100 mins (Success)",
        "ProvisionSmsBundle MSISDN={MSISDN} amount=500 SMS (Success)",
        "DebitPrepaidBalance MSISDN={MSISDN} amount=250 SYP usage=VOICE (Success)",
        "CreateInProfile MSISDN={MSISDN} profile=YA_HALA_PREPAID (Success)",
        "ProvisionDataBundle MSISDN={MSISDN} amount=1GB nightly booster (Success)",
        "ProvisionVoiceQuota MSISDN={MSISDN} amount=50 mins international (Success)",
    ];

    private static string[] BuildHlrMessageTemplates() =>
    [
        "RegisterImsiMapping IMSI={IMSI} ICCID={ICCID} (Success)",
        "ActivateServiceCode CLIP=TRUE MSISDN={MSISDN} (Success)",
        "ActivateServiceCode Voice=TRUE SMS=TRUE Data=TRUE MSISDN={MSISDN} (Success)",
        "HlrCreateSubscriber MSISDN={MSISDN} IMSI={IMSI} ICCID={ICCID} plan=PREPAID (Success)",
        "HSS_Profile_Migration completed for MSISDN={MSISDN} (Success)",
        "ActivateServiceCode CLIR=FALSE MSISDN={MSISDN} (Success)",
        "RegisterImsiMapping IMSI={IMSI} ICCID={ICCID} LTE_APN=internet.syriatel (Success)",
        "ActivateServiceCode VoLTE=TRUE MSISDN={MSISDN} (Success)",
        "HlrSimProfileUpdate MSISDN={MSISDN} IMSI={IMSI} ICCID={ICCID} (Success)",
        "ActivateServiceCode GPRS=TRUE MSISDN={MSISDN} APN=wap.syriatel (Success)",
        "HSS_Profile_Migration completed for MGR-0001 MSISDN={MSISDN} (Success)",
        "ActivateServiceCode Roaming=FALSE MSISDN={MSISDN} (Success)",
    ];
}
