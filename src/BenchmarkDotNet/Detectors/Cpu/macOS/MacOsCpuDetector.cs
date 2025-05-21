using BenchmarkDotNet.Helpers;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.macOS;

/// <summary>
/// CPU information from output of the `sysctl -a` command.
/// MacOSX only.
/// </summary>
internal class MacOsCpuDetector : ICpuDetector
{
    public bool IsApplicable() => OsDetector.IsMacOS();

    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        string sysctlOutput = ProcessHelper.RunAndReadOutput("sysctl", "-a");
        return SysctlCpuInfoParser.Parse(sysctlOutput);
    }
}