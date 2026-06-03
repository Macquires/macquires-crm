using Application.Features.TelecomManager.Commands;
using Domain.Entities;

namespace Application.Common.Telecom.SellingLine;

public interface ISellingLineEligibilityChecker
{
    Task ValidateForCreateAsync(CreateTelecomOperationRequest request, CancellationToken cancellationToken);

    Task ValidateForConfirmAsync(TelecomOperationRequest operation, CancellationToken cancellationToken);

    Task ValidateCatalogForCreateAsync(
        CreateTelecomOperationRequest request,
        string resolvedProductId,
        string? resolvedOfferingId,
        CancellationToken cancellationToken);
}
