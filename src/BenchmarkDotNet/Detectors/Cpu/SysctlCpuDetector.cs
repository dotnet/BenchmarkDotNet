using System;
using BenchmarkDotNet.Helpers;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu;

/// <summary>
/// CPU information from output of the `sysctl -a` command.
/// MacOSX only.
/// </summary>
internal static class SysctlCpuDetector
{
    internal static readonly Lazy<PhdCpu> Cpu = new (Load);

    private static PhdCpu? Load()
    {
        if (OsDetector.IsMacOS())
        {
            string content = ProcessHelper.RunAndReadOutput("sysctl", "-a");
            return SysctlCpuParser.Parse(content);
        }
        return null;
    }
}