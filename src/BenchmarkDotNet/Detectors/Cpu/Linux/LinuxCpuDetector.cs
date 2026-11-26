using BenchmarkDotNet.Helpers;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Linux;

/// <summary>
/// CPU information from output of the `cat /proc/cpuinfo` and `lscpu` command.
/// When the process is elevated, the CPU name and frequencies are overwritten that are taken from SMBIOS information.
/// </summary>
internal class LinuxCpuDetector : CpuDetectorBase
{
    public override bool IsApplicable() => OsDetector.IsLinux();

    public override CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        // lscpu output respects the system locale, so we should force language invariant environment for correct parsing
        var languageInvariantEnvironment = new Dictionary<string, string>
        {
            ["LC_ALL"] = "C",
            ["LANG"] = "C",
            ["LANGUAGE"] = "C"
        };

        string? cpuInfo = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo") ?? string.Empty;
        string? lscpu = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu\"", environmentVariables: languageInvariantEnvironment) ?? string.Empty;

        var result = LinuxCpuInfoParser.Parse(cpuInfo, lscpu);

        // Try to overwrite ProcessorName/Frequencies information by using SMBIOS data
        if (TryGetSmbiosCpuInfo(out var smbiosCpuInfo))
            ApplySmbiosNameAndFrequency(result, smbiosCpuInfo);

        return result;
    }
}
