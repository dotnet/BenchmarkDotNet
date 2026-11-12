using System.Diagnostics;
using System.Reflection;

namespace BenchmarkDotNet.Extensions
{
    internal static class AssemblyExtensions
    {
        internal static bool? IsJitOptimizationDisabled(this Assembly? assembly)
            => GetDebuggableAttribute(assembly)?.IsJITOptimizerDisabled;

        internal static bool? IsDebug(this Assembly? assembly)
            => GetDebuggableAttribute(assembly) is { } debuggableAttribute
                ? debuggableAttribute.IsJITTrackingEnabled && debuggableAttribute.IsJITOptimizerDisabled
                : null;

        internal static bool IsTrue(this bool? valueOrNothing) => valueOrNothing.HasValue && valueOrNothing.Value;

        private static DebuggableAttribute? GetDebuggableAttribute(Assembly? assembly)
        {
            try
            {
                return assembly?.GetCustomAttributes().OfType<DebuggableAttribute>().SingleOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
