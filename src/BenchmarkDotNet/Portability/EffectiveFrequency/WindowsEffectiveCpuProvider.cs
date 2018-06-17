using System;
using BenchmarkDotNet.Portability.Cpu;

namespace BenchmarkDotNet.Portability.EffectiveFrequency
{
    public class WindowsEffectiveCpuProvider
    {
        internal static readonly Lazy<CpuInfo> WindowsEffectiveCpuInfo = new Lazy<CpuInfo>(GetInfo);
        
        private static CpuInfo GetInfo()
        {
            if (RuntimeInformation.IsWindows() && RuntimeInformation.IsFullFramework && !RuntimeInformation.IsMono)
                return new CpuInfo(MosCpuInfoProvider.MosCpuInfo.Value.ProcessorName,
                                   MosCpuInfoProvider.MosCpuInfo.Value.PhysicalProcessorCount,
                                   MosCpuInfoProvider.MosCpuInfo.Value.PhysicalCoreCount,
                                   MosCpuInfoProvider.MosCpuInfo.Value.LogicalCoreCount,
                                   MosCpuInfoProvider.MosCpuInfo.Value.NominalFrequency,
                                   MosCpuInfoProvider.MosCpuInfo.Value.MinFrequency,
                                   MosCpuInfoProvider.MosCpuInfo.Value.MaxFrequency,
                                   MosCpuInfoProvider.MosCpuInfo.Value.NominalFrequency);
            if (RuntimeInformation.IsWindows())
                return new CpuInfo(WmicCpuInfoProvider.WmicCpuInfo.Value.ProcessorName,
                                   WmicCpuInfoProvider.WmicCpuInfo.Value.PhysicalProcessorCount,
                                   WmicCpuInfoProvider.WmicCpuInfo.Value.PhysicalCoreCount,
                                   WmicCpuInfoProvider.WmicCpuInfo.Value.LogicalCoreCount,
                                   WmicCpuInfoProvider.WmicCpuInfo.Value.NominalFrequency,
                                   WmicCpuInfoProvider.WmicCpuInfo.Value.MinFrequency,
                                   WmicCpuInfoProvider.WmicCpuInfo.Value.MaxFrequency,
                                   WmicCpuInfoProvider.WmicCpuInfo.Value.NominalFrequency);
            return null;
        }
    }
}