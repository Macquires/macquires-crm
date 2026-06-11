using Domain.Entities;

namespace Application.Common.Telecom.BackOffice;

public sealed record BackOfficePaymentValidationResult(
    bool IsValid,
    string MessageAr,
    string? ValidationCode = null);

/// <summary>Cross-checks reconnect payment references against the billing journal before BO approval.</summary>
public interface IBackOfficePaymentReferenceValidator
{
    Task<BackOfficePaymentValidationResult> ValidateReconnectPaymentAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}
