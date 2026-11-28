using BenchmarkDotNet.Helpers;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.MacOS;

/// <summary>
/// CPU information from output of the `sysctl -a` command.
/// When SMBIOS data is available (x64), the CPU name and frequencies are overwritten with the SMBIOS values.
/// </summary>
internal class MacOsCpuDetector : CpuDetectorBase
{
    public override bool IsApplicable() => OsDetector.IsMacOS();

    public override CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        string? sysctlOutput = ProcessHelper.RunAndReadOutput("sysctl", "-a");

        if (sysctlOutput is null)
            return null;

        var result = SysctlCpuInfoParser.Parse(sysctlOutput);

        // Try to overwrite ProcessorName/Frequencies information by using SMBIOS data
        if (TryGetSmbiosCpuInfo(out var smbiosCpuInfo))
            ApplySmbiosNameAndFrequency(result, smbiosCpuInfo);

        return result;
    }
}
