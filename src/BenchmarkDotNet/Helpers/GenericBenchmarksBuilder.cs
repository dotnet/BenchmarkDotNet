using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Helpers
{
    internal static class GenericBenchmarksBuilder
    {
        internal static Type[] GetRunnableBenchmarks(IEnumerable<Type> types)
            => BuildRunnableBenchmarks(types)
                    .Where(x => x.IsSuccess)
                    .Select(x => x.Type)
                    .ToArray();

        /// <summary>
        /// Builds the benchmark types of the given types, keeping the ones that could not be built so that a caller
        /// with somewhere to report them can.
        /// </summary>
        /// <param name="types">The types to consider.</param>
        /// <returns>Every type that was built, and every one that was rejected.</returns>
        internal static IEnumerable<GenericBenchmarkType> BuildRunnableBenchmarks(IEnumerable<Type> types)
            => types.Where(type => type.ContainsRunnableBenchmarks())
                    .SelectMany(BuildGenericsIfNeeded);

        internal static IEnumerable<GenericBenchmarkType> BuildGenericsIfNeeded(Type type)
        {
            if (!TryGetGenericTypeArguments(type, out var typeArguments, out var error))
                return [GenericBenchmarkType.Unreadable(type, error)];

            if (typeArguments.Length > 0)
                return BuildGenericTypes(type, typeArguments);

            return [GenericBenchmarkType.Runnable(type)];
        }

        /// <summary>
        /// Reads the [GenericTypeArguments] of a type, if the attributes of that type can be read at all.
        /// </summary>
        /// <remarks>
        /// Reflection constructs every attribute of a type in order to hand any of them back, so a single attribute
        /// whose constructor throws makes the whole read throw: [Config(typeof(SomeAbstractConfig))] fails inside
        /// ConfigAttribute's own constructor, and there is no way to ask for the [GenericTypeArguments] alone. The
        /// type is unusable once that happens - BenchmarkConverter would throw on the very same read - so it is
        /// reported as a failure and dropped, rather than aborting the enumeration of every other benchmark in the
        /// assembly.
        /// </remarks>
        /// <param name="type">The type to read the attributes of.</param>
        /// <param name="typeArguments">The type argument sets the type is to be closed over.</param>
        /// <param name="error">The reason the attributes could not be read.</param>
        /// <returns>Whether the attributes could be read.</returns>
        private static bool TryGetGenericTypeArguments(Type type, out Type[][] typeArguments, out string error)
        {
            try
            {
                typeArguments = type.GetCustomAttributes(true).OfType<GenericTypeArgumentsAttribute>()
                                                              .Select(x => x.GenericTypeArguments)
                                                              .ToArray();
                error = string.Empty;
                return true;
            }
            catch (Exception e)
            {
                typeArguments = [];
                error = $"Type {type.Name} was ignored because its attributes could not be read: {e.Message}";
                return false;
            }
        }

        private static IEnumerable<GenericBenchmarkType> BuildGenericTypes(Type type, IEnumerable<Type[]> typeArguments)
            => typeArguments.Select(genericArg => type.TryMakeGenericType(genericArg, out var builtType)
                ? GenericBenchmarkType.Runnable(builtType)
                : GenericBenchmarkType.Failed(builtType, $"Generic type {builtType.Name} failed to build due to wrong type argument or arguments count, ignoring."));

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
