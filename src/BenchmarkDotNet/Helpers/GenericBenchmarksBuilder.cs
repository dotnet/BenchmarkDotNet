using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Helpers
{
    internal static class GenericBenchmarksBuilder
    {
        [UnconditionalSuppressMessage(category: "Trimming", checkId: "IL2067", Justification = AotJustifications.DiscoveredBenchmarkTypes)]
        internal static Type[] GetRunnableBenchmarks(IEnumerable<Type> types)
            => types.Where(type => type.ContainsRunnableBenchmarks())
                    .SelectMany(BuildGenericsIfNeeded)
                    .Where(x => x.isSuccess)
                    .Select(x => x.result)
                    .ToArray();

        internal static IEnumerable<(bool isSuccess, Type result)> BuildGenericsIfNeeded(Type type)
        {
            var typeArguments = type.GetCustomAttributes(true).OfType<GenericTypeArgumentsAttribute>()
                                                              .Select(x => x.GenericTypeArguments)
                                                              .ToArray();

            if (typeArguments.Length != 0)
                return BuildGenericTypes(type, typeArguments);

            return [(true, type)];
        }

        private static IEnumerable<(bool isSuccess, Type result)> BuildGenericTypes(Type type, IEnumerable<Type[]> typeArguments)
            => typeArguments.Select(genericArg => (type.TryMakeGenericType(genericArg, out var builtType), builtType));

        [UnconditionalSuppressMessage(category: "Trimming", checkId: "IL2055", Justification = AotJustifications.DiscoveredBenchmarkTypes)]
        [UnconditionalSuppressMessage(category: "AOT", checkId: "IL3050", Justification = AotJustifications.DiscoveredBenchmarkTypes)]
        private static bool TryMakeGenericType(this Type type, Type[] typeArguments, out Type result)
        {
            try
            {
                result = type.MakeGenericType(typeArguments);
                return true;
            }
            catch (ArgumentException) // thrown when number or type of generic arguments is invalid, https://msdn.microsoft.com/en-us/library/system.type.makegenerictype(v=vs.110).aspx?f=255&mspperror=-2147217396#Anchor_1
            {
                result = type;
                return false;
            }
        }
    }
}