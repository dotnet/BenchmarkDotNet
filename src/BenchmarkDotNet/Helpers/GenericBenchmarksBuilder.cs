using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Helpers
{
    internal static class GenericBenchmarksBuilder
    {
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

            if (typeArguments.Any())
                return BuildGenericTypes(type, typeArguments);

            return new (bool isSuccess, Type result)[] { (true,  type) };
        }

        private static IEnumerable<(bool isSuccess, Type result)> BuildGenericTypes(Type type, IEnumerable<Type[]> typeArguments)
            => typeArguments.Select(genericArg => (type.TryMakeGenericType(genericArg, out var builtType), builtType));

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