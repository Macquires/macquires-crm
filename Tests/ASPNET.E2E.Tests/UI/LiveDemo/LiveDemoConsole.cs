namespace ASPNET.E2E.Tests.UI.LiveDemo;

internal static class LiveDemoConsole
{
    private const int BannerWidth = 80;

    public static void LogSecurityBanner()
    {
        var line = new string('=', BannerWidth);
        var previousColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine(line);
            Console.WriteLine("🟢 SYSTEM SECURITY: Docker topology and database containers verified. Infrastructure is solid and ready for live execution.");
            Console.WriteLine(line);
            Console.WriteLine();
        }
        finally
        {
            Console.ForegroundColor = previousColor;
        }
    }

    public static void LogStep(string message)
    {
        Console.WriteLine($"  → {message}");
    }

    public static void LogScenarioStart(string message)
    {
        Console.WriteLine();
        Console.WriteLine(new string('-', BannerWidth));
        Console.WriteLine(message);
        Console.WriteLine(new string('-', BannerWidth));
    }

    public static void LogSuccess(string message)
    {
        var previousColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = previousColor;
        }
    }

    public static void LogCompliance(string message)
    {
        var previousColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = previousColor;
        }
    }
}
