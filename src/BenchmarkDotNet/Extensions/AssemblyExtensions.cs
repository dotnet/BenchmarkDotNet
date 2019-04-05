using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Extensions
{
    internal static class AssemblyExtensions
    {
        internal static bool? IsJitOptimizationDisabled(this Assembly assembly)
            => GetDebuggableAttribute(assembly).IsJitOptimizerDisabled();

        internal static bool? IsDebug(this Assembly assembly)
            => GetDebuggableAttribute(assembly).IsJitTrackingEnabled();

        internal static bool IsTrue(this bool? valueOrNothing) => valueOrNothing.HasValue && valueOrNothing.Value;

        private static DebuggableAttribute GetDebuggableAttribute(Assembly assembly)
            => assembly?.GetCustomAttributes()
                .OfType<DebuggableAttribute>()
                .SingleOrDefault();

        private static bool? IsJitOptimizerDisabled(this DebuggableAttribute attribute) => attribute?.IsJITOptimizerDisabled;

        private static bool? IsJitTrackingEnabled(this DebuggableAttribute attribute) => attribute?.IsJITTrackingEnabled;
    }
}