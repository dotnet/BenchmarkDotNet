using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    public class SysctlCpuInfoParser : ICpuInfo
    {
        public SysctlCpuInfoParser(string content)
        {
            var sysctl = StringHelper.Parse(content, ':');
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
    }
}