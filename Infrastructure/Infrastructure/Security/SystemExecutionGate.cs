using Application.Common.Security;

namespace Infrastructure.Security;

public sealed class SystemExecutionGate : ISystemExecutionGate
{
    private bool _isActive;

    public bool IsActive => _isActive;

    public IDisposable Enter()
    {
        _isActive = true;
        return new Scope(this);
    }

    private sealed class Scope(SystemExecutionGate gate) : IDisposable
    {
        public void Dispose() => gate._isActive = false;
    }
}
