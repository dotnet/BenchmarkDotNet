using BenchmarkDotNet.Detectors.Cpu.Windows;
using BenchmarkDotNet.Detectors.Smbios;
using Perfolizer.Models;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Detectors.Cpu;

internal abstract class CpuDetectorBase : ICpuDetector
{
    public abstract bool IsApplicable();

    public abstract CpuInfo? Detect();

    protected static bool TryGetSmbiosCpuInfo([NotNullWhen(true)] out CpuInfo? cpuInfo)
    {
        if (!SmbiosDataAccessor.TryGetSmbiosData(out _, out var smbiosData))
        {
            cpuInfo = null;
            return false;
        }

        var processors = SmbiosDataParser.ParseProcessorInfoTable(smbiosData);
        cpuInfo = SmbiosCpuDetector.FromProcessorInfo(processors);

        return cpuInfo != null;
    }

    internal static CpuInfo ApplySmbiosNameAndFrequency(CpuInfo cpuInfo, CpuInfo smbiosCpuInfo)
    {
        // Only the name and the frequencies come from SMBIOS.
        if (smbiosCpuInfo.ProcessorName is not null)
            cpuInfo.ProcessorName = smbiosCpuInfo.ProcessorName;
        if (smbiosCpuInfo.MaxFrequencyHz is not null)
            cpuInfo.MaxFrequencyHz = smbiosCpuInfo.MaxFrequencyHz;
        if (smbiosCpuInfo.NominalFrequencyHz is not null)
            cpuInfo.NominalFrequencyHz = smbiosCpuInfo.NominalFrequencyHz;

        return cpuInfo;
    }
}
