using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Telecom.Termination;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class TerminationEligibilityConfirmTests
{
    [Fact]
    public async Task ValidateForConfirmAsync_throws_when_termination_type_missing()
    {
        var checker = new TerminationEligibilityChecker(null!, new StubBilling());
        var op = new TelecomOperationRequest
        {
            Id = "op-1",
            SubscriberProfileId = "prof-1",
            MsisdnAssetId = "msisdn-1",
            TerminationReason = "Test",
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => checker.ValidateForConfirmAsync(op));
        Assert.Contains("نوع الإنهاء", ex.Message);
    }

    [Fact]
    public async Task ValidateForConfirmAsync_throws_when_msisdn_missing()
    {
        var checker = new TerminationEligibilityChecker(null!, new StubBilling());
        var op = new TelecomOperationRequest
        {
            Id = "op-1",
            SubscriberProfileId = "prof-1",
            TerminationType = TerminationWellKnown.Voluntary,
            TerminationReason = "Test",
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => checker.ValidateForConfirmAsync(op));
        Assert.Contains("رقم الخط", ex.Message);
    }

    private sealed class StubBilling : IBillingSystemIntegration
    {
        public Task<decimal> GetOutstandingBalanceAsync(string msisdn, CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task AdjustBalanceAsync(string msisdn, decimal newBalance, string? reason = null, string? idempotencyKey = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<BillingProvisionResult> ProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BillingProvisionResult> ReverseProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BillingRechargeResult> RechargeAsync(BillingRechargeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BillingRechargeResult> ReverseRechargeAsync(BillingReverseRechargeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
