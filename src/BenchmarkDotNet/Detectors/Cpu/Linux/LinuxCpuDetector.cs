using System.Collections.Generic;
using BenchmarkDotNet.Helpers;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Linux;

/// <summary>
/// CPU information from output of the `cat /proc/cpuinfo` and `lscpu` command.
/// Linux only.
/// </summary>
internal class LinuxCpuDetector : ICpuDetector
{
    public bool IsApplicable() => OsDetector.IsLinux();

    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        // lscpu output respects the system locale, so we should force language invariant environment for correct parsing
        var languageInvariantEnvironment = new Dictionary<string, string>
        {
            ["LC_ALL"] = "C",
            ["LANG"] = "C",
            ["LANGUAGE"] = "C"
        };

        string? cpuInfo = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo");
        string? lscpu = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu\"", environmentVariables: languageInvariantEnvironment);

        if (cpuInfo is null || lscpu is null)
            return null;

        return LinuxCpuInfoParser.Parse(cpuInfo, lscpu);
    }
}