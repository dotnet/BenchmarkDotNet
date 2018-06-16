using System;
using System.Diagnostics;
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
                return ProcCpuInfoParser.ParseOutput(content);
            }
            return null;
        }

        private static string GetProcessorSpeed()
        {

            return "";
        }
        
        private static string CPUSpeedLinuxWithDummy()
        {
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

                var output = ProcessHelper.RunAndReadOutput("/bin/bash","-c \"lscpu | grep MHz\"");
                
                process.Close();
                return output;
            }
        }
    }
}