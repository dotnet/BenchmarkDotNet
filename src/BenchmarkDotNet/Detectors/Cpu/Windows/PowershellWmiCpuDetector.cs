using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        bool checkForPowershell7Plus = true;

        if (Directory.Exists(powershell7PlusPath))
        {
            //Use .Last so that we get the newest major PowerShell version
           string[] subDirectories = Directory.EnumerateDirectories(powershell7PlusPath, "*", SearchOption.AllDirectories)
               .ToArray();

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

        const string argList = $"{WmicCpuInfoKeyNames.Name}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfCores}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfLogicalProcessors}, " +
                               $"{WmicCpuInfoKeyNames.MaxClockSpeed}";

        string powershellPath;

        // Optimistically, use Cross-platform new PowerShell when available but fallback to Windows PowerShell if not available.
        if (checkForPowershell7Plus)
        {
            powershellPath = File.Exists(powershell7PlusPath) ? powershell7PlusPath : windowsPowershellPath;
        }
        else
        {
            powershellPath = windowsPowershellPath;
        }

        if (File.Exists(powershellPath) == false)
            powershellPath = "PowerShell";

        string output = ProcessHelper.RunAndReadOutput(powershellPath, "Get-CimInstance Win32_Processor -Property " + argList);
        return PowershellWmiCpuInfoParser.Parse(output);
    }
}