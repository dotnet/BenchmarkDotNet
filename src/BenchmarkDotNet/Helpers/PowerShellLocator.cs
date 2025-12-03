using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Detectors;

namespace BenchmarkDotNet.Helpers;

/// <summary>
/// Locates PowerShell on a system, currently only supports on Windows.
/// </summary>
internal class PowerShellLocator
{
    private static readonly string WindowsPowershellPath =
        $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}WindowsPowerShell{Path.DirectorySeparatorChar}" +
        $"v1.0{Path.DirectorySeparatorChar}powershell.exe";

        [SupportedOSPlatform("windows")]
    internal static string? LocateOnWindows()
    {
        if (OsDetector.IsWindows() == false)
            return null;

        string powershellPath;

        try
        {
            string programFiles = Environment.Is64BitOperatingSystem
                ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string powershell7PlusPath = $"{programFiles}{Path.DirectorySeparatorChar}Powershell{Path.DirectorySeparatorChar}";

            bool checkForPowershell7Plus = true;

            if (Directory.Exists(powershell7PlusPath))
            {
                string[] subDirectories = Directory.EnumerateDirectories(powershell7PlusPath, "*", SearchOption.AllDirectories)
                    .ToArray();

                //Use the highest number string directory for PowerShell so that we get the newest major PowerShell version
                // Example version directories are 6, 7, and in the future 8.
                string? subDirectory = subDirectories.Where(x => Regex.IsMatch(x, "[0-9]"))
                    .Select(x => x)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                if (subDirectory is not null)
                {
                    powershell7PlusPath = $"{subDirectory}{Path.DirectorySeparatorChar}pwsh.exe";
                }
                else
                {
                    checkForPowershell7Plus = false;
                }
            }

            // Optimistically, use Cross-platform new PowerShell when available but fallback to Windows PowerShell if not available.
            if (checkForPowershell7Plus)
            {
                powershellPath = File.Exists(powershell7PlusPath) ? powershell7PlusPath : WindowsPowershellPath;
            }
            else
            {
                powershellPath = WindowsPowershellPath;
            }

            if (File.Exists(powershellPath) == false)
                powershellPath = "PowerShell";
        }
        catch
        {
            powershellPath = "PowerShell";
        }

        return powershellPath;
    }
}