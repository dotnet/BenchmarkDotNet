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
                var output = CpuSpeedLinuxWithDummy();
                content = content + output;
                return ProcCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
        
        private static string CpuSpeedLinuxWithDummy()
        {
            var output = ProcessHelper.RunAndReadOutput("/bin/bash","-c \"lscpu | grep \"max MHz\"\"")?
                                      .Split('\n')
                                      .First()
                                      .Split(' ')
                                      .Last();
            
            return $"\ncpu freq\t:{output}";
        }
    }
}