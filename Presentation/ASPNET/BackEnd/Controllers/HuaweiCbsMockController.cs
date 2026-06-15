using Application.Common.Repositories;
using ASPNET.BackEnd.Common.Attributes;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ASPNET.BackEnd.Controllers;

[ApiController]
[Route("api/huawei-cbs-mock")]
[RequireTelecomAdminSettings]
public class HuaweiCbsMockController : ControllerBase
{
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HuaweiCbsMockController> _logger;

    public HuaweiCbsMockController(
        ICommandRepository<BillingIntegrationLog> logRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IUnitOfWork unitOfWork,
        ILogger<HuaweiCbsMockController> logger)
    {
        _logRepository = logRepository;
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("QueryBalance")]
    public async Task<IActionResult> QueryBalanceAsync([FromQuery] string msisdn, CancellationToken cancellationToken)
    {
        // 1. Simulate Latency
        var delayMs = new Random().Next(500, 1500);
        await Task.Delay(delayMs, cancellationToken);

        // 2. Normalize and check MSISDN (Syriatel numbers start with 093 or 099)
        var cleanMsisdn = (msisdn ?? "").Trim();
        if (cleanMsisdn.Length == 10 && cleanMsisdn.StartsWith("09", StringComparison.Ordinal))
        {
            cleanMsisdn = "093" + cleanMsisdn[2..];
        }

        bool isValidSyriatel = cleanMsisdn.StartsWith("093", StringComparison.Ordinal)
            || cleanMsisdn.StartsWith("099", StringComparison.Ordinal);

        if (!isValidSyriatel)
        {
            await TryWriteLogAsync(null, $"QueryBalance failed for MSISDN {cleanMsisdn} - Subscriber Not Found (Code 20001)", false, cancellationToken);
            return Ok(new
            {
                success = false,
                errorCode = 20001,
                message = $"Subscriber Not Found: {cleanMsisdn} is not provisioned on Huawei CBS."
            });
        }

        // 3. Generate random balance for Demo (5,000 to 50,000 SYP)
        var balance = new Random().Next(5000, 50000);

        await TryWriteLogAsync(null, $"QueryBalance succeeded for MSISDN {cleanMsisdn} - Balance: {balance} SYP", true, cancellationToken);

        return Ok(new
        {
            success = true,
            msisdn = cleanMsisdn,
            balance = balance,
            currency = "SYP",
            status = "Active",
            simulatedDelayMs = delayMs
        });
    }

    [HttpPost("Recharge")]
    public async Task<IActionResult> RechargeAsync(
        [FromBody] RechargeRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        [FromServices] Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        // S18 — Idempotency check
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            if (cache.TryGetValue(idempotencyKey, out object? cachedObj) && cachedObj is IActionResult cachedResponse)
            {
                return cachedResponse;
            }
        }

        // 0. Validate amount
        var amount = request?.Amount ?? 0;
        if (amount <= 0)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = 10001,
                message = "مبلغ الشحن يجب أن يكون أكبر من صفر."
            });
        }

        // 1. Simulate Latency
        var delayMs = new Random().Next(500, 1500);
        await Task.Delay(delayMs, cancellationToken);

        var cleanMsisdn = (request?.Msisdn ?? "").Trim();
        bool isValidSyriatel = cleanMsisdn.StartsWith("093") || cleanMsisdn.StartsWith("099");

        if (!isValidSyriatel)
        {
            await TryWriteLogAsync(null, $"Recharge of {amount} SYP failed for MSISDN {cleanMsisdn} - Subscriber Not Found (Code 20001)", false, cancellationToken);
            var errResp = Ok(new
            {
                success = false,
                errorCode = 20001,
                message = $"Recharge failed: Subscriber {cleanMsisdn} Not Found."
            });
            if (!string.IsNullOrEmpty(idempotencyKey)) cache.Set(idempotencyKey, errResp, TimeSpan.FromMinutes(5));
            return errResp;
        }

        var newBalance = new Random().Next(5000, 15000) + amount;

        await TryWriteLogAsync(null, $"Recharge of {amount} SYP succeeded for MSISDN {cleanMsisdn}. New balance: {newBalance} SYP", true, cancellationToken);

        var okResp = Ok(new
        {
            success = true,
            msisdn = cleanMsisdn,
            rechargedAmount = amount,
            newBalance = newBalance,
            message = $"Top-up successful! {amount} SYP has been added to {cleanMsisdn}.",
            simulatedDelayMs = delayMs
        });
        if (!string.IsNullOrEmpty(idempotencyKey)) cache.Set(idempotencyKey, okResp, TimeSpan.FromMinutes(5));
        return okResp;
    }

    [HttpPost("SubscribeBundle")]
    public async Task<IActionResult> SubscribeBundleAsync([FromBody] SubscribeBundleRequest request, CancellationToken cancellationToken)
    {
        // 0. Validate bundle code
        if (string.IsNullOrWhiteSpace(request?.BundleCode))
        {
            return BadRequest(new
            {
                success = false,
                errorCode = 10002,
                message = "رمز الباقة مطلوب."
            });
        }

        // 1. Simulate Latency
        var delayMs = new Random().Next(500, 1500);
        await Task.Delay(delayMs, cancellationToken);

        var cleanMsisdn = (request?.Msisdn ?? "").Trim();
        bool isValidSyriatel = cleanMsisdn.StartsWith("093") || cleanMsisdn.StartsWith("099");

        if (!isValidSyriatel)
        {
            await TryWriteLogAsync(null, $"Subscribe bundle {request?.BundleCode} failed for MSISDN {cleanMsisdn} - Subscriber Not Found (Code 20001)", false, cancellationToken);
            return Ok(new
            {
                success = false,
                errorCode = 20001,
                message = $"Bundle activation failed: Subscriber {cleanMsisdn} Not Found."
            });
        }

        // S19 — Simulate insufficient balance (deterministic by hash for demo consistency)
        var simulatedBalance = new Random(cleanMsisdn.GetHashCode()).Next(5000, 50000);
        var bundleCost = new Random(request!.BundleCode.GetHashCode()).Next(10000, 40000);
        if (simulatedBalance < bundleCost)
        {
            await TryWriteLogAsync(null, $"Subscribe bundle {request.BundleCode} failed for MSISDN {cleanMsisdn} - Insufficient Balance ({simulatedBalance} < {bundleCost} SYP)", false, cancellationToken);
            return Ok(new
            {
                success = false,
                errorCode = 20003,
                message = $"رصيد غير كافٍ لتفعيل الباقة. الرصيد الحالي: {simulatedBalance:N0} ل.س، سعر الباقة: {bundleCost:N0} ل.س.",
                currentBalance = simulatedBalance,
                bundleCost = bundleCost,
                simulatedDelayMs = delayMs
            });
        }

        await TryWriteLogAsync(null, $"Subscribe bundle {request.BundleCode} succeeded for MSISDN {cleanMsisdn}", true, cancellationToken);

        return Ok(new
        {
            success = true,
            msisdn = cleanMsisdn,
            bundleCode = request?.BundleCode,
            message = $"Package activation successful! Bundle {request?.BundleCode} is now active on {cleanMsisdn}.",
            simulatedDelayMs = delayMs
        });
    }

    private async Task TryWriteLogAsync(string? operationId, string message, bool success, CancellationToken cancellationToken)
    {
        try
        {
            string targetOperationId = operationId ?? "";

            if (string.IsNullOrEmpty(targetOperationId))
            {
                var anyOp = await _operationRepository.GetQuery().FirstOrDefaultAsync(cancellationToken);
                if (anyOp != null)
                {
                    targetOperationId = anyOp.Id;
                }
            }

            if (!string.IsNullOrEmpty(targetOperationId))
            {
                var operation = await _operationRepository.GetQuery()
                    .Where(o => o.Id == targetOperationId)
                    .Select(o => new { o.BranchId })
                    .FirstOrDefaultAsync(cancellationToken);

                var previousAttempts = await _logRepository.GetQuery()
                    .Where(l => l.TelecomOperationRequestId == targetOperationId)
                    .Select(l => l.AttemptNumber)
                    .ToListAsync(cancellationToken);

                var nextAttempt = previousAttempts.Any() ? previousAttempts.Max() + 1 : 1;

                var log = new BillingIntegrationLog
                {
                    TelecomOperationRequestId = targetOperationId,
                    AttemptNumber = nextAttempt,
                    Success = success,
                    Message = message,
                    IntegrationTarget = "HuaweiCBS-Mock",
                    BranchId = operation?.BranchId
                };
                await _logRepository.CreateAsync(log, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HuaweiCBS-Mock: failed to write billing integration log for operation {OperationId}", operationId);
        }
    }
}

public class RechargeRequest
{
    public string Msisdn { get; set; } = null!;
    public decimal Amount { get; set; }
}

public class SubscribeBundleRequest
{
    public string Msisdn { get; set; } = null!;
    public string BundleCode { get; set; } = null!;
}
