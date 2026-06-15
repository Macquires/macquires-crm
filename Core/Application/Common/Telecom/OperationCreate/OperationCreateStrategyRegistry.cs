using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public interface IOperationCreateStrategyRegistry
{
    IOperationCreateStrategy Resolve(TelecomOperationKind kind);
}

public sealed class OperationCreateStrategyRegistry : IOperationCreateStrategyRegistry
{
    private readonly IReadOnlyDictionary<TelecomOperationKind, IOperationCreateStrategy> _strategies;

    public OperationCreateStrategyRegistry(IEnumerable<IOperationCreateStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Kind);
    }

    public IOperationCreateStrategy Resolve(TelecomOperationKind kind) =>
        _strategies.TryGetValue(kind, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"No create strategy registered for {kind}.");
}
