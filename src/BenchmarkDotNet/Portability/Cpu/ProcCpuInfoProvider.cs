using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    /// <summary>
    /// CPU information from output of the `cat /proc/info` command.
    /// Linux only.
    /// </summary>
    internal static class ProcCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> ProcCpuInfo = new Lazy<CpuInfo>(Load);

        [CanBeNull]
        private static CpuInfo Load()
        {
            if (RuntimeInformation.IsLinux())
            {
                string content = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo");
                var output = GetCpuSpeed();
                content = content + output;
                return ProcCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
        
        private static string GetCpuSpeed()
        {
            var output = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu | grep MHz\"")?
                                      .Split('\n')
                                      .SelectMany(x => x.Split(':'))
                                      .ToArray();
            
            if (output == null || output.Length < 6)
                return null;
                
            var current = double.TryParse(output[1].Trim(), out double currentValue);
            var max = double.TryParse(output[3].Trim().Replace(',', '.'), out double maxValue);
            var min = double.TryParse(output[5].Trim().Replace(',', '.'), out double minValue);
            
            return $"\n{ProcCpuInfoKeyNames.NominalFrequency}\t:{currentValue}\n{ProcCpuInfoKeyNames.MinFrequency}\t:{minValue}\n{ProcCpuInfoKeyNames.MaxFrequency}\t:{maxValue}\n";
        }
    }
}