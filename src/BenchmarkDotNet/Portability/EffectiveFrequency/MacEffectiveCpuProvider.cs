using System;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability.Cpu;

namespace BenchmarkDotNet.Portability.EffectiveFrequency
{
    public class MacEffectiveCpuProvider
    {
        internal static readonly Lazy<CpuInfo> MacEffectiveCpuInfo = new Lazy<CpuInfo>(GetInfo);
        
        private static CpuInfo GetInfo()
        {
            
            if (RuntimeInformation.IsMacOSX())
            {
                string content = ProcessHelper.RunAndReadOutput("sysctl", "-a");
                var cpuInfo = SysctlCpuInfoParser.ParseOutput(content);
                return new CpuInfo(cpuInfo.ProcessorName,
                                   cpuInfo.PhysicalProcessorCount,
                                   cpuInfo.PhysicalCoreCount,
                                   cpuInfo.LogicalCoreCount,
                                   cpuInfo.NominalFrequency,
                                   cpuInfo.MinFrequency,
                                   cpuInfo.MaxFrequency,
                                   cpuInfo.NominalFrequency);
            }
            return null;
        }
    }
}