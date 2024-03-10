using System;
using BenchmarkDotNet.Detectors.Cpu;
using BenchmarkDotNet.Portability;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors;

public class CpuDetector
{
    private static readonly CpuDetector Instance = new ();
    private CpuDetector() { }

    private readonly Lazy<PhdCpu?> cpu = new (ResolveCpu);
    public static PhdCpu? GetCpu() => Instance.cpu.Value;

    private static PhdCpu? ResolveCpu()
    {
        var cpu = ResolveCpuBasic();
        if (cpu != null)
        {
            cpu.Architecture = RuntimeInformation.GetArchitecture();
            cpu.SetDisplay();
        }
        return cpu;
    }

    private static PhdCpu? ResolveCpuBasic()
    {
        if (OsDetector.IsWindows() && Portability.RuntimeInformation.IsFullFramework && !Portability.RuntimeInformation.IsMono)
            return MosCpuDetector.Cpu.Value;
        if (OsDetector.IsWindows())
            return WmicCpuDetector.Cpu.Value;
        if (OsDetector.IsLinux())
            return ProcCpuDetector.Cpu.Value;
        if (OsDetector.IsMacOS())
            return SysctlCpuDetector.Cpu.Value;
        return null;
    }
}