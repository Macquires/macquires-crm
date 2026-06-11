using Domain.Enums;

namespace Application.Common.Telecom.OperationConfirm;

public interface IOperationConfirmStrategyRegistry
{
    IOperationConfirmStrategy? Resolve(TelecomOperationKind kind);
}

public sealed class OperationConfirmStrategyRegistry : IOperationConfirmStrategyRegistry
{
    private readonly IReadOnlyDictionary<TelecomOperationKind, IOperationConfirmStrategy> _strategies;

    public OperationConfirmStrategyRegistry(IEnumerable<IOperationConfirmStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Kind);
    }

    public IOperationConfirmStrategy? Resolve(TelecomOperationKind kind) =>
        _strategies.TryGetValue(kind, out var strategy) ? strategy : null;
}
