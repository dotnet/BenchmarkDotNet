using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Portability.Cpu.Linux;

/// <summary>
/// CPU information from output of the `cat /proc/cpuinfo` and `lscpu` command.
/// Linux only.
/// </summary>
internal class LinuxCpuInfoDetector : ICpuInfoDetector
{
    public bool IsApplicable() => RuntimeInformation.IsLinux();

    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        string cpuInfo = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo") ?? "";
        string lscpu = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu\"");
        return LinuxCpuInfoParser.Parse(cpuInfo, lscpu);
    }
}