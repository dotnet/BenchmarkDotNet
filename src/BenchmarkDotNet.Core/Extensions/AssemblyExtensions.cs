using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Extensions
{
    internal static class AssemblyExtensions
    {
        internal static bool? IsJITOptimizationDisabled(this Assembly assembly)
        {
#if CORE
            return null; // https://github.com/dotnet/coreclr/pull/6153
#else
            return assembly?.GetCustomAttributes(false)
                .OfType<DebuggableAttribute>()
                .SingleOrDefault()?.IsJITOptimizerDisabled;
#endif
        }

        internal static bool? IsDebug(this Assembly assembly)
        {
#if CORE
            return null; // https://github.com/dotnet/coreclr/pull/6153
#else
            return assembly?.GetCustomAttributes(false)
                .OfType<DebuggableAttribute>()
                .SingleOrDefault()?.IsJITTrackingEnabled;
#endif
        }

        internal static bool IsTrue(this bool? valueOrNothing) => valueOrNothing.HasValue && valueOrNothing.Value;
    }
}