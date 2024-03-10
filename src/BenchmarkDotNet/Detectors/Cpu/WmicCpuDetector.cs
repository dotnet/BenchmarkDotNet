using System;
using System.IO;
using BenchmarkDotNet.Helpers;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu;

/// <summary>
/// CPU information from output of the `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List` command.
/// Windows only.
/// </summary>
internal static class WmicCpuDetector
{
    internal static readonly Lazy<PhdCpu> Cpu = new (Load);

    private const string DefaultWmicPath = @"C:\Windows\System32\wbem\WMIC.exe";

    private static PhdCpu? Load()
    {
        if (OsDetector.IsWindows())
        {
            const string argList = $"{WmicCpuKeyNames.Name}, {WmicCpuKeyNames.NumberOfCores}, " +
                                   $"{WmicCpuKeyNames.NumberOfLogicalProcessors}, {WmicCpuKeyNames.MaxClockSpeed}";
            string wmicPath = File.Exists(DefaultWmicPath) ? DefaultWmicPath : "wmic";
            string content = ProcessHelper.RunAndReadOutput(wmicPath, $"cpu get {argList} /Format:List");
            return WmicCpuParser.ParseOutput(content);
        }
        return null;
    }
}