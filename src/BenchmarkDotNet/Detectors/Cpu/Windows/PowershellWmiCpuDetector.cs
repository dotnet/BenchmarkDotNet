using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Helpers;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

/// <summary>
/// CPU information from output of the `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List` command.
/// Windows only.
/// </summary>
internal class PowershellWmiCpuDetector : ICpuDetector
{
    private readonly string windowsPowershellPath =
        $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}WindowsPowerShell{Path.DirectorySeparatorChar}" +
        $"v1.0{Path.DirectorySeparatorChar}powershell.exe";

    public bool IsApplicable() => OsDetector.IsWindows();

    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        string programFiles = Environment.Is64BitOperatingSystem ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        string powershell7PlusPath = $"{programFiles}{Path.DirectorySeparatorChar}Powershell{Path.DirectorySeparatorChar}";

        if (Directory.Exists(powershell7PlusPath))
        {
            //Use .Last so that we get the newest major PowerShell version
            string subDirectory = Directory.EnumerateDirectories(powershell7PlusPath, "^[a-zA-Z]", SearchOption.AllDirectories).Last();

            powershell7PlusPath = $"{subDirectory}{Path.DirectorySeparatorChar}pwsh.exe";
        }
        const string argList = $"{WmicCpuInfoKeyNames.Name}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfCores}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfLogicalProcessors}, " +
                               $"{WmicCpuInfoKeyNames.MaxClockSpeed}";

        // Optimistically, use Cross-platform new PowerShell when available but fallback to Windows PowerShell if not available.
        string powershellPath = File.Exists(windowsPowershellPath) ? powershell7PlusPath : windowsPowershellPath;

        if (File.Exists(powershellPath) == false)
            return null;

        string output = ProcessHelper.RunAndReadOutput(powershellPath, "Get-CimInstance Win32_Processor -Property " + argList);
        return PowershellWmiCpuInfoParser.Parse(output);
    }
}