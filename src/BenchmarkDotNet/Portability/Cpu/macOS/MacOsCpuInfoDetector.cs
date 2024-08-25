using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Portability.Cpu.macOS;

/// <summary>
/// CPU information from output of the `sysctl -a` command.
/// MacOSX only.
/// </summary>
internal class MacOsCpuInfoDetector : ICpuInfoDetector
{
    public bool IsApplicable() => RuntimeInformation.IsMacOS();

    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        string sysctlOutput = ProcessHelper.RunAndReadOutput("sysctl", "-a");
        return SysctlCpuInfoParser.Parse(sysctlOutput);
    }
}