using System.IO;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

/// <summary>
/// CPU information from output of the `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List` command.
/// Windows only.
/// </summary>
internal class WmicCpuDetector : ICpuDetector
{
    private const string DefaultWmicPath = @"C:\Windows\System32\wbem\WMIC.exe";

    public bool IsApplicable() => OsDetector.IsWindows();

    public PhdCpu? Detect()
    {
        if (!IsApplicable()) return null;

        const string argList = $"{WmicCpuInfoKeyNames.Name}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfCores}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfLogicalProcessors}, " +
                               $"{WmicCpuInfoKeyNames.MaxClockSpeed}";
        string wmicPath = File.Exists(DefaultWmicPath) ? DefaultWmicPath : "wmic";
        string wmicOutput = ProcessHelper.RunAndReadOutput(wmicPath, $"cpu get {argList} /Format:List");
        return WmicCpuInfoParser.Parse(wmicOutput);
    }
}