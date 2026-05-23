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

        var exePath = PowerShellLocator.LocateOnWindows() ?? "powershell";
        var command = $"Get-CimInstance Win32_Processor -Property {argList} | Format-List {argList}";
        var envVars = new Dictionary<string, string>
        {
            ["TERM"] = "dumb" // Suppress outputting ANSI escape sequences.
        };

        string? output = ProcessHelper.RunAndReadOutput(exePath, $"-Command \"{command}\"", environmentVariables: envVars);

        if (output.IsBlank())
            return null;

        return CpuInfoParser.ParseCimOutput(output);
    }
}