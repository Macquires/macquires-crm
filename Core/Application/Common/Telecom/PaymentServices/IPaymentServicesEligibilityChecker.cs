using Domain.Entities;

namespace Application.Common.Telecom.PaymentServices;

public interface IPaymentServicesEligibilityChecker
{
    Task EnsureCanCreateRechargeAsync(
        string customerId,
        string subscriptionId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task EnsureGatewayReferenceUniqueAsync(
        string gatewayReference,
        string? excludePaymentId,
        CancellationToken cancellationToken = default);

    void EnsureCanConfirm(TelecomPaymentTransaction payment);

    Task EnsureCanReverseAsync(TelecomPaymentTransaction payment, CancellationToken cancellationToken = default);

    Task EnsureRechargeVelocityAsync(string msisdn, string? actorUserId, CancellationToken cancellationToken = default);
}
