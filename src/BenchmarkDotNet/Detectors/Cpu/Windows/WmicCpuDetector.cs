using System.IO;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

/// <summary>
/// CPU information from output of the `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List` command.
/// Windows only.
/// </summary>
/// <remarks>WMIC is deprecated by Microsoft starting with Windows 10 21H1 (including Windows Server), and it is not known whether it still ships with Windows by default.
/// <para>WMIC may be removed in a future version of Windows. See <see href="https://learn.microsoft.com/en-us/windows/win32/wmisdk/wmic"/> </para></remarks>
internal class WmicCpuDetector : ICpuDetector
{
    private const string DefaultWmicPath = @"C:\Windows\System32\wbem\WMIC.exe";

    public bool IsApplicable() => OsDetector.IsWindows();

    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        const string argList = $"{WmicCpuInfoKeyNames.Name}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfCores}, " +
                               $"{WmicCpuInfoKeyNames.NumberOfLogicalProcessors}, " +
                               $"{WmicCpuInfoKeyNames.MaxClockSpeed}";
        string wmicPath = File.Exists(DefaultWmicPath) ? DefaultWmicPath : "wmic";
        string? wmicOutput = ProcessHelper.RunAndReadOutput(wmicPath, $"cpu get {argList} /Format:List");

        if (string.IsNullOrEmpty(wmicOutput))
            return null;

        return WmicCpuInfoParser.Parse(wmicOutput);
    }
}