using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;
#if !NETCOREAPP2_1
using BenchmarkDotNet.Extensions;
#endif

namespace BenchmarkDotNet.Portability.Memory
{
    internal static class SysctlMemoryInfoParser
    {
        [CanBeNull]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        internal static MemoryInfo ParseOutput([CanBeNull] string content)
        {
            var sysctl = SectionsHelper.ParseSection(content, ':');
            var totalMemory = GetLongValue(sysctl, "hw.memsize");
            var freePhysicalMemory = GetLongValue(sysctl, "hw.usermem");

            if (totalMemory.HasValue && freePhysicalMemory.HasValue)
            {
                return new MemoryInfo(totalMemory.Value, freePhysicalMemory.Value);
            }
            else
            {
                return null;
            }
        }

        [CanBeNull]
        private static long? GetLongValue([NotNull] Dictionary<string, string> sysctl, [NotNull] string keyName)
        {
            if (sysctl.TryGetValue(keyName, out string value) && long.TryParse(value, out long result))
            {
                return result;
            }

            return null;
        }
    }
}