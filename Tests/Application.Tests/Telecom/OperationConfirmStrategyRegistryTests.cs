using Application.Common.Telecom.OperationConfirm;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class OperationConfirmStrategyRegistryTests
{
    [Fact]
    public void Registry_resolves_all_telecom_operation_kinds()
    {
        var kinds = Enum.GetValues<TelecomOperationKind>();
        var registry = new OperationConfirmStrategyRegistry(
            kinds.Select(k => new StubStrategy(k)).Cast<IOperationConfirmStrategy>());

        foreach (var kind in kinds)
        {
            Assert.NotNull(registry.Resolve(kind));
            Assert.Equal(kind, registry.Resolve(kind)!.Kind);
        }
    }

    private sealed class StubStrategy(TelecomOperationKind kind) : IOperationConfirmStrategy
    {
        public TelecomOperationKind Kind => kind;

        public bool RequiresNetworkProvision => false;

        public Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
            Domain.Entities.TelecomOperationRequest entity,
            string? actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new OperationConfirmValidationResult(true, "ok"));

        public Task<OperationApplyResult?> ApplyLocalChangesAsync(
            Domain.Entities.TelecomOperationRequest entity,
            string? actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OperationApplyResult?>(new OperationApplyResult("093", null, null, null, null));
    }
}
