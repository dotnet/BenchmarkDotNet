using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

/// <summary>
/// CPU information from output of the `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List` command.
/// Windows only.
/// </summary>
/// <remarks>
/// WMIC is not installed by default on Windows 11 25H2.
/// It has been announced that it will be completely removed from Windows 11 in the next Windows feature update.
/// <para> See <see href="https://support.microsoft.com/en-us/topic/windows-management-instrumentation-command-line-wmic-removal-from-windows-e9e83c7f-4992-477f-ba1d-96f694b8665d"/> </para>
/// <para> See <see href="https://learn.microsoft.com/en-us/windows/win32/wmisdk/wmic"/> </para>
/// </remarks>
internal class WmicCpuDetector : ICpuDetector
{
    private const string DefaultWmicPath = @"C:\Windows\System32\wbem\WMIC.exe";

    public bool IsApplicable() => OsDetector.IsWindows();

    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        const string argList = $"{WmiCpuInfoKeyNames.Name}, " +
                               $"{WmiCpuInfoKeyNames.NumberOfCores}, " +
                               $"{WmiCpuInfoKeyNames.NumberOfLogicalProcessors}, " +
                               $"{WmiCpuInfoKeyNames.MaxClockSpeed}";
        string wmicPath = File.Exists(DefaultWmicPath) ? DefaultWmicPath : "wmic";
        string? wmicOutput = ProcessHelper.RunAndReadOutput(wmicPath, $"cpu get {argList} /Format:List");

        if (wmicOutput.IsBlank())
            return null;

        return WmiCpuInfoParser.Parse(wmicOutput);
    }
}