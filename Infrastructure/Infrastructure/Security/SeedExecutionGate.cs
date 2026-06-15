namespace Infrastructure.Security;

public interface ISeedExecutionGate
{
    bool IsActive { get; }

    IDisposable Enter();
}

/// <summary>Scoped flag so <see cref="IOperatorContext"/> resolves to <see cref="SeedOperatorContext"/> during seed.</summary>
public sealed class SeedExecutionGate : ISeedExecutionGate
{
    private bool _isActive;

    public bool IsActive => _isActive;

    public IDisposable Enter()
    {
        _isActive = true;
        return new Scope(this);
    }

    private sealed class Scope(SeedExecutionGate gate) : IDisposable
    {
        public void Dispose() => gate._isActive = false;
    }
}
