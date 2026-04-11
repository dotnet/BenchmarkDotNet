using BenchmarkDotNet.Extensions;
using System.Diagnostics;
using System.Reflection;

namespace BenchmarkDotNet.Detectors;

internal static class AppHostDetector
{
    public static bool HasAppHost()
    {
        var entryAssembly = Assembly.GetEntryAssembly();

        // Check NativeAOT
        if (entryAssembly == null)
            return false;

        using var process = Process.GetCurrentProcess();
        var processName = process.ProcessName;

        // Check executed with `dotnet run`
        if (processName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            return false;

        // Check Single-file executable (AppHost is bundled)
        if (entryAssembly.Location.IsBlank())
            return true;

        // Check entry assembly extension.
        if (entryAssembly.Location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return true;

        // Return false for unknown environment
        return false;
    }
}
