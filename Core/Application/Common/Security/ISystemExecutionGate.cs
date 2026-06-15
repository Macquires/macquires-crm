namespace Application.Common.Security;

/// <summary>Scoped flag so <see cref="IOperatorContext"/> resolves to the system worker identity during background jobs.</summary>
public interface ISystemExecutionGate
{
    bool IsActive { get; }

    IDisposable Enter();
}
