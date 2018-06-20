using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability.Cpu;

namespace BenchmarkDotNet.Portability.EffectiveFrequency
{
    internal class LinuxEffectiveCpuProvider
    {
        internal static readonly Lazy<CpuInfo> LinuxEffectiveCpuInfo = new Lazy<CpuInfo>(GetInfo);
        
        private static CpuInfo GetInfo()
        {
            var cpuInfo = ProcCpuInfoProvider.ProcCpuInfo.Value;
            var dummyMeasureResultList = CpuSpeedLinuxWithDummy();
            var dummyMeasureResult = dummyMeasureResultList.Max();
            if (dummyMeasureResultList.Count > 0
                && dummyMeasureResult > 0)
                return new CpuInfo(cpuInfo.ProcessorName,
                                   cpuInfo.PhysicalProcessorCount,
                                   cpuInfo.PhysicalCoreCount,
                                   cpuInfo.LogicalCoreCount,
                                   cpuInfo.NominalFrequency,
                                   cpuInfo.MinFrequency,
                                   cpuInfo.MaxFrequency,
                                   dummyMeasureResult);
            
            return null;
        }
        
        private static List<double> CpuSpeedLinuxWithDummy()
        {
            var list = new List<double>();
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                WorkingDirectory = "",
                Arguments = "-c \"while (( 1 )); do echo busy; done\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            using (var process = new Process { StartInfo = processStartInfo })
            {
                try
                {
                    process.Start();
                }
                catch (Exception)
                {
                    return null;
                }

                for (int i = 0; i < 16; i++)
                {
                    var output = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu | grep 'CPU MHz'\"")?.Split(':');
                    if (output != null && double.TryParse(output[1].Trim(), out double currentValue))
                        list.Add(currentValue);
                }
                
                process.Kill();
                return list;
            }
        }
    }
}