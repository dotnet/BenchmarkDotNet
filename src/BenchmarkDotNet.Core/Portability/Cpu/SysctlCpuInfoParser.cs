using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    internal static class SysctlCpuInfoParser
    {
        [NotNull]
        internal static CpuInfo ParseOutput([CanBeNull] string content)
        {
            var sysctl = SectionsHelper.ParseSection(content, ':');
            string processorName = sysctl.GetValueOrDefault("machdep.cpu.brand_string");
            var physicalProcessorCount = GetPositiveIntValue(sysctl, "hw.packages");
            var physicalCoreCount = GetPositiveIntValue(sysctl, "hw.physicalcpu");
            var logicalCoreCount = GetPositiveIntValue(sysctl, "hw.logicalcpu");
            return new CpuInfo(processorName, physicalProcessorCount, physicalCoreCount, logicalCoreCount);
        }

        [CanBeNull]
        private static int? GetPositiveIntValue([NotNull] Dictionary<string, string> sysctl, [NotNull] string keyName)
        {
            if (sysctl.TryGetValue(keyName, out string value) &&
                int.TryParse(value, out int result) &&
                result > 0)
                return result;
            return null;
        }
    }
}