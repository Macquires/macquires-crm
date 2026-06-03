using Application.Common.Telecom.PaymentServices;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class PaymentServicesEligibilityTests
{
    private sealed class NoOpFraudTickets : IPaymentServicesFraudTicketService
    {
        public Task EnqueueRechargeVelocityFraudTicketAsync(
            string msisdn,
            string? customerId,
            string? subscriberProfileId,
            int rechargeCount,
            string? actorUserId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void EnsureCanConfirm_rejects_completed_payment()
    {
        var checker = new PaymentServicesEligibilityChecker(null!, new NoOpFraudTickets());
        var payment = new TelecomPaymentTransaction { Status = PaymentTransactionStatus.Completed };

        var ex = Assert.Throws<Application.Common.Exceptions.BusinessRuleViolationException>(
            () => checker.EnsureCanConfirm(payment));

        Assert.Contains("منتهية", ex.Message);
    }

    [Fact]
    public async Task EnsureCanReverse_rejects_non_completed()
    {
        var checker = new PaymentServicesEligibilityChecker(null!, new NoOpFraudTickets());
        var payment = new TelecomPaymentTransaction { Status = PaymentTransactionStatus.Draft };

        var ex = await Assert.ThrowsAsync<Application.Common.Exceptions.BusinessRuleViolationException>(
            () => checker.EnsureCanReverseAsync(payment));

        Assert.Contains("VAL-12-05", ex.Message);
    }
}