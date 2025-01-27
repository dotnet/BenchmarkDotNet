using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu.macOS;

/// <summary>
/// CPU information from output of the `sysctl -a` command.
/// MacOSX only.
/// </summary>
internal class MacOsCpuDetector : ICpuDetector
{
    public bool IsApplicable() => OsDetector.IsMacOS();

    public PhdCpu? Detect()
    {
        if (!IsApplicable()) return null;

        string sysctlOutput = ProcessHelper.RunAndReadOutput("sysctl", "-a");
        return SysctlCpuInfoParser.Parse(sysctlOutput);
    }
}