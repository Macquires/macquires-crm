namespace ASPNET.E2E.Tests.UI.LiveDemo;

public sealed class LiveDemoInfrastructureException : Exception
{
    public const string CriticalMessage =
        "❌ CRITICAL FAILURE: Docker Desktop or the containerized Database is offline! Please ensure Docker Desktop is running before starting the demo.";

    public LiveDemoInfrastructureException()
        : base(CriticalMessage)
    {
    }

    public LiveDemoInfrastructureException(string message)
        : base(message)
    {
    }
}
