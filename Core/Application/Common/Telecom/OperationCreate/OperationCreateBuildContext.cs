using Domain.Entities;

namespace Application.Common.Telecom.OperationCreate;

public sealed class OperationCreateBuildContext
{
    public OperationCreateBuildContext(
        OperationCreateContext validation,
        TelecomOperationRequest entity,
        string? simInventoryId,
        string? branchId,
        string actorUserId)
    {
        Validation = validation;
        Entity = entity;
        SimInventoryId = simInventoryId;
        BranchId = branchId;
        ActorUserId = actorUserId;
    }

    public OperationCreateContext Validation { get; }
    public TelecomOperationRequest Entity { get; }
    public string? SimInventoryId { get; set; }
    public string? BranchId { get; }
    public string ActorUserId { get; }
}
