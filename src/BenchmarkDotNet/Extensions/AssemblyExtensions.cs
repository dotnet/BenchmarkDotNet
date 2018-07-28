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
        {
            return assembly?.GetCustomAttributes()
                            .OfType<DebuggableAttribute>()
                            .SingleOrDefault();
        }

        private static bool? IsJitOptimizerDisabled(this DebuggableAttribute attribute) => Read(attribute, "IsJITOptimizerDisabled");

        private static bool? IsJitTrackingEnabled(this DebuggableAttribute attribute) => Read(attribute, "IsJITTrackingEnabled");

        private static bool? Read(DebuggableAttribute debuggableAttribute, string propertyName)
        {
            // the properties are implemented (https://github.com/dotnet/coreclr/pull/6153)
            // but not exposed in corefx Contracts due to .NET Standard versioning problems (https://github.com/dotnet/corefx/issues/13717)
            // so we need to use reflection to read this simple property...
            var propertyInfo = typeof(DebuggableAttribute).GetProperty(propertyName);
            if (debuggableAttribute == null || propertyInfo == null)
                return null;

            return (bool)propertyInfo.GetValue(debuggableAttribute);
        }
    }
}