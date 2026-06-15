using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public interface IOperationCreateStrategy
{
    TelecomOperationKind Kind { get; }

    Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken);

    Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken);

    Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken);

    OperationCreatePostCreateFlags PostCreateFlags { get; }
}
