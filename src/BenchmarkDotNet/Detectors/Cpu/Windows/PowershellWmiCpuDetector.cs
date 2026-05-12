using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Models;
using System.Runtime.Versioning;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

/// <summary>
/// CPU information from output of the `Get-CimInstance Win32_Processor -Property Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed` command.
/// Windows only.
/// </summary>
internal class PowershellWmiCpuDetector : ICpuDetector
{
    public bool IsApplicable() => OsDetector.IsWindows();

    [SupportedOSPlatform("windows")]
    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        const string argList = $"{WmiCpuInfoKeyNames.Name}, " +
                               $"{WmiCpuInfoKeyNames.NumberOfCores}, " +
                               $"{WmiCpuInfoKeyNames.NumberOfLogicalProcessors}, " +
                               $"{WmiCpuInfoKeyNames.MaxClockSpeed}";

        string? output = ProcessHelper.RunAndReadOutput(PowerShellLocator.LocateOnWindows() ?? "PowerShell",
            $"Get-CimInstance Win32_Processor -Property {argList} | Format-List {argList}");

        if (output.IsBlank())
            return null;

        return CpuInfoParser.ParseCimOutput(output);
    }
}