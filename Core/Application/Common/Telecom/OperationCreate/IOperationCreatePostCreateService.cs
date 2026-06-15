using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public interface IOperationCreatePostCreateService
{
    Task RunAsync(
        TelecomOperationRequest entity,
        string actorUserId,
        CancellationToken cancellationToken);
}
