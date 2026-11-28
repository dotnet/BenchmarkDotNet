using BenchmarkDotNet.Detectors.Smbios;
using BenchmarkDotNet.Extensions;
using Perfolizer.Models;
using System.Runtime.Versioning;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

/// <summary>
/// Gets CPU information from the SMBIOS Type 4 (Processor Information) structures.
/// </summary>
internal class SmbiosCpuDetector : ICpuDetector
{
    public bool IsApplicable()
    {
        if (OsDetector.IsWindows())
            return true;

        return false;
    }

    [SupportedOSPlatform("windows6.1")]
    [SupportedOSPlatform("linux")]
    public CpuInfo? Detect()
    {
        if (!IsApplicable())
            return null;

        try
        {
            var processorInfo = SmbiosDataAccessor.GetProcessorInfo();
            return FromProcessorInfo(processorInfo);
        }
        catch
        {
            return null; // Fallback to use other CpuDetector.
        }
    }

    internal static CpuInfo? FromProcessorInfo(IEnumerable<SmbiosProcessorInfo> cpus)
    {
        var processorModelNames = new HashSet<string>();
        int physicalProcessorCount = 0;
        int physicalCoreCount = 0;
        int logicalCoreCount = 0;
        long maxFrequencyMHz = 0;
        long nominalFrequencyMHz = 0;

        foreach (SmbiosProcessorInfo cpu in cpus)
        {
            // A populated socket can still have its CPU disabled by the user or the BIOS,
            // and only enabled CPUs contribute to the detected CPU information.
            if (!cpu.IsEnabled)
                continue;

            physicalProcessorCount++;

            var name = cpu.ProcessorVersion;
            if (name.IsNotBlank())
                processorModelNames.Add(name.Trim());

            if (cpu.ResolvedCoreCount > 0)
                physicalCoreCount += cpu.ResolvedCoreCount;
            else if (cpu.ResolvedCoreEnabledCount > 0)
                physicalCoreCount += cpu.ResolvedCoreEnabledCount;

            // Fall back to Thread Enabled when Thread Count is unknown.
            if (cpu.ResolvedThreadCount > 0)
                logicalCoreCount += cpu.ResolvedThreadCount;
            else if (cpu.ThreadEnabledCount > 0)
                logicalCoreCount += cpu.ThreadEnabledCount;

            // TODO: Need to distinguish P-Core/E-Core
            if (cpu.MaxSpeedMHz > 0)
                maxFrequencyMHz = Math.Max(maxFrequencyMHz, cpu.MaxSpeedMHz);

            if (cpu.CurrentSpeedMHz > 0)
                nominalFrequencyMHz = nominalFrequencyMHz == 0
                    ? cpu.CurrentSpeedMHz
                    : Math.Min(nominalFrequencyMHz, cpu.CurrentSpeedMHz);
        }

        // No enabled socket means SMBIOS cannot provide usable CPU information.
        if (physicalProcessorCount == 0)
            return null;

        // Fall back to MaxSpeed when CurrentSpeed is unknown (and vice versa).
        if (nominalFrequencyMHz == 0)
            nominalFrequencyMHz = maxFrequencyMHz;
        if (maxFrequencyMHz == 0)
            maxFrequencyMHz = nominalFrequencyMHz;

        string? processorName = processorModelNames.Count > 0
            ? string.Join(", ", processorModelNames)
            : null;

        return new CpuInfo
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = physicalProcessorCount,
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : null,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = nominalFrequencyMHz > 0
                ? nominalFrequencyMHz * 1_000_000
                : null,
            MaxFrequencyHz = maxFrequencyMHz > 0
                ? maxFrequencyMHz * 1_000_000
                : null,
        };
    }
}
