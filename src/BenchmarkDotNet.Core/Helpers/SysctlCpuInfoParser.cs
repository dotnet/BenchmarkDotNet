using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers
{
    public class SysctlCpuInfoParser : ICpuInfo
    {
        public SysctlCpuInfoParser(string content)
        {
            var sysctl = ParseList(content, ':');
            PhysicalProcessorCount = GetPositiveIntValue(sysctl, "hw.packages");
            PhysicalCoreCount = GetPositiveIntValue(sysctl, "hw.logicalcpu");
            LogicalCoreCount = GetPositiveIntValue(sysctl, "hw.physicalcpu");
            ProcessorName = sysctl.GetValueOrDefault("machdep.cpu.brand_string");
        }

        public int? PhysicalCoreCount { get; }
        public int? PhysicalProcessorCount { get; }
        public int? LogicalCoreCount { get; }
        public string ProcessorName { get; }

        [CanBeNull]
        private static int? GetPositiveIntValue([NotNull] Dictionary<string, string> sysctl, string keyName)
        {
            if (sysctl.TryGetValue(keyName, out string value) &&
                int.TryParse(value, out int result) &&
                result > 0)
                return result;
            return null;
        }

        [NotNull]
        private static Dictionary<string, string> ParseList([CanBeNull] string content, char separator)
        {
            var values = new Dictionary<string, string>();
            var list = content?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (list != null)
                foreach (string line in list)
                    if (line.IndexOf(separator) != -1)
                    {
                        var lineParts = line.Split(separator);
                        if (lineParts.Length >= 2)
                            values[lineParts[0].Trim()] = lineParts[1].Trim();
                    }
            return values;
        }
    }
}