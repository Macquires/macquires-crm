using System.Diagnostics;

namespace ASPNET.E2E.Tests.Infrastructure;

internal static class DockerProbe
{
    internal static bool IsAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(TimeSpan.FromSeconds(8));
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
