using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Portability.Cpu
{
    /// <summary>
    /// CPU information from output of the `cat /proc/info` command.
    /// Linux only.
    /// </summary>
    internal static class ProcCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> ProcCpuInfo = new (Load);

        private static CpuInfo? Load()
        {
            if (RuntimeInformation.IsLinux())
            {
                string content = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo") ?? "";
                string output = GetCpuSpeed() ?? "";
                content += output;
                return ProcCpuInfoParser.ParseOutput(content);
            }
            return null;
        }

        private static string? GetCpuSpeed()
        {
            try
            {
                string[]? output = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu | grep MHz\"")?
                    .Split('\n')
                    .SelectMany(x => x.Split(':'))
                    .ToArray();

                return ParseCpuFrequencies(output);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string? ParseCpuFrequencies(string[]? input)
        {
            // Example of output we trying to parse:
            //
            // CPU MHz: 949.154
            // CPU max MHz: 3200,0000
            // CPU min MHz: 800,0000

            if (input == null)
                return null;

            var output = new StringBuilder();
            for (int i = 0; i + 1 < input.Length; i += 2)
            {
                string name = input[i].Trim();
                string value = input[i + 1].Trim();

                if (name.EqualsWithIgnoreCase("CPU min MHz"))
                    if (Frequency.TryParseMHz(value.Replace(',', '.'), out var minFrequency))
                        output.Append($"\n{ProcCpuInfoKeyNames.MinFrequency}\t:{minFrequency.ToMHz()}");

                if (name.EqualsWithIgnoreCase("CPU max MHz"))
                    if (Frequency.TryParseMHz(value.Replace(',', '.'), out var maxFrequency))
                        output.Append($"\n{ProcCpuInfoKeyNames.MaxFrequency}\t:{maxFrequency.ToMHz()}");
            }

            return output.ToString();
        }
    }
}