using BenchmarkDotNet.Detectors;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Helpers;

/// <summary>
/// Locates PowerShell on a system, currently only supports on Windows.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class PowerShellLocator
{
    private const string FallbackCommand = "powershell";

    private static readonly string WindowsPowerShellPath =
        Path.Combine(
            Environment.SystemDirectory,
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");

    private static readonly Lazy<string> CachedExePath = new(LocateOnWindowsCore);

    internal static string LocateOnWindows()
        => CachedExePath.Value;

    private static string LocateOnWindowsCore()
    {
        if (!OsDetector.IsWindows())
            throw new PlatformNotSupportedException();

        try
        {
            // Try to find PowerShell Core (`pwsh.exe`)
            if (TryLocatePwshExe(out var pwshExePath))
                return pwshExePath;

            // Try to find Windows PowerShell (`powershell.exe)
            if (File.Exists(WindowsPowerShellPath))
                return WindowsPowerShellPath;
        }
        catch
        {
            // ignored
        }

        // Fallback to `powershell` command, which should be available in PATH.
        return FallbackCommand;
    }

    private static bool TryLocatePwshExe([NotNullWhen(true)] out string? pwshExePath)
    {
        pwshExePath = null;

        string programFiles = Environment.Is64BitOperatingSystem
            ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        string root = Path.Combine(programFiles, "PowerShell");

        if (!Directory.Exists(root))
            return false;

        // Find latest version of PowerShell Core install directory.(e.g. `C:\Program Files\PowerShell\7`)
        var latestVersionDirectory = Directory
            .EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
            .Where(path => Regex.IsMatch(Path.GetFileName(path), @"^\d+$"))
            .OrderByDescending(path => int.Parse(Path.GetFileName(path)))
            .FirstOrDefault();

        if (latestVersionDirectory is null)
            return false;

        pwshExePath = Path.Combine(latestVersionDirectory, "pwsh.exe");
        return File.Exists(pwshExePath);
    }
}
