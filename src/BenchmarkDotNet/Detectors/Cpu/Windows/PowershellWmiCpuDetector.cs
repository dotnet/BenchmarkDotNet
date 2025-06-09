using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
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

    #if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    #endif
    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        const string argList = $"{WmicCpuInfoKeyNames.Name}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfCores}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfLogicalProcessors}, " +
                               $"{WmicCpuInfoKeyNames.MaxClockSpeed}";

        string output = ProcessHelper.RunAndReadOutput(PowerShellLocator.LocateOnWindows() ?? "PowerShell",
            "Get-CimInstance Win32_Processor -Property " + argList);

        if (string.IsNullOrEmpty(output))
            return null;

        return PowershellWmiCpuInfoParser.Parse(output);
    }
}