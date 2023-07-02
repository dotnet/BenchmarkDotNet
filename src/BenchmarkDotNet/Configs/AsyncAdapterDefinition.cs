using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Configs
{
    public sealed class AsyncAdapterDefinition : IComparable<AsyncAdapterDefinition>, IEquatable<AsyncAdapterDefinition>
    {
        public Type AwaitableType { get; private set; }
        public Type AwaitableAdapterType { get; private set; }
        public Type AsyncMethodBuilderAdapterType { get; private set; }

        private readonly Type[] awaitableAdapterInterfaceGenericArguments;
        private readonly Type[] originalAwaitableGenericArguments;
        private readonly int openGenericArgumentsCount;

        public AsyncAdapterDefinition(Type awaitableAdapterType, Type? asyncMethodBuilderAdapterType = null)
        {
            // If asyncMethodBuilderAdapterType is null, we use awaitableAdapterType if it implements IAsyncMethodBuilderAdapter,
            // otherwise we fallback to AsyncTaskMethodBuilderAdapter.
            asyncMethodBuilderAdapterType ??= awaitableAdapterType.GetInterfaces().Contains(typeof(IAsyncMethodBuilderAdapter))
                ? awaitableAdapterType
                : typeof(AsyncTaskMethodBuilderAdapter);
            AwaitableAdapterType = awaitableAdapterType;
            AsyncMethodBuilderAdapterType = asyncMethodBuilderAdapterType;

            // Validate asyncMethodBuilderAdapterType
            bool isPublic = asyncMethodBuilderAdapterType.IsPublic || asyncMethodBuilderAdapterType.IsNestedPublic;
            if (!isPublic || (!asyncMethodBuilderAdapterType.IsValueType && asyncMethodBuilderAdapterType.GetConstructor(Array.Empty<Type>()) == null))
            {
                throw new ArgumentException($"asyncMethodBuilderAdapterType [{asyncMethodBuilderAdapterType.GetCorrectCSharpTypeName()}] is not a public struct, or a public class with a public, parameterless constructor.");
            }
            if (!asyncMethodBuilderAdapterType.GetInterfaces().Contains(typeof(IAsyncMethodBuilderAdapter)))
            {
                throw new ArgumentException($"asyncMethodBuilderAdapterType [{asyncMethodBuilderAdapterType.GetCorrectCSharpTypeName()}] does not implement [{typeof(IAsyncMethodBuilderAdapter).GetCorrectCSharpTypeName()}].");
            }

            // Validate awaitableAdapterType
            isPublic = asyncMethodBuilderAdapterType.IsPublic || asyncMethodBuilderAdapterType.IsNestedPublic;
            if (!isPublic || (!asyncMethodBuilderAdapterType.IsValueType && asyncMethodBuilderAdapterType.GetConstructor(Array.Empty<Type>()) == null))
            {
                throw new ArgumentException($"awaitableAdapterType [{asyncMethodBuilderAdapterType.GetCorrectCSharpTypeName()}] is not a public struct, or a public class with a public, parameterless constructor.");
            }

            // Must implement exactly 1 IAwaitableAdapter
            var awaitableAdapterVoidInterfaces = awaitableAdapterType.GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAwaitableAdapter<,>)).ToArray();
            var awaitableAdapterResultInterfaces = awaitableAdapterType.GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAwaitableAdapter<,,>)).ToArray();
            if (awaitableAdapterVoidInterfaces.Length + awaitableAdapterResultInterfaces.Length != 1)
            {
                string msg = awaitableAdapterVoidInterfaces.Length + awaitableAdapterResultInterfaces.Length == 0
                    ? "does not implement"
                    : "implements more than one of";
                throw new ArgumentException($"awaitableAdapterType [{awaitableAdapterType.GetCorrectCSharpTypeName()}] {msg} [{typeof(IAwaitableAdapter<,>).GetCorrectCSharpTypeName()}] or [{typeof(IAwaitableAdapter<,,>).GetCorrectCSharpTypeName()}].");
            }

            // Retrieve the awaitable type from the IAwaitableAdapter<TAwaitable,> interface
            awaitableAdapterInterfaceGenericArguments = awaitableAdapterVoidInterfaces.Length == 1
                ? awaitableAdapterVoidInterfaces[0].GetGenericArguments()
                : awaitableAdapterResultInterfaces[0].GetGenericArguments();

            Type awaitableType = awaitableAdapterInterfaceGenericArguments[0];

            // Validate open generics
            // awaitableType and awaitableAdapterType must contain the exact same open generic types,
            // while asyncMethodBuilderAdapterType can have the same or fewer open generic types.
            // It doesn't matter if an open generic type is used multiple times (<T, T, T>), as long as each distinct type matches.
            originalAwaitableGenericArguments = awaitableType.GetGenericArguments();
            var awaitableOpenGenericTypes = new HashSet<Type>(
                originalAwaitableGenericArguments.Where(t => t.IsGenericParameter)
            );
            Type[] builderGenericTypes = asyncMethodBuilderAdapterType.GetGenericArguments();
            var builderOpenGenericTypes = new HashSet<Type>(
                builderGenericTypes.Where(t => t.IsGenericParameter)
            );
            Type[] awaitableAdapterGenericTypes = awaitableAdapterType.GetGenericArguments();
            foreach (var genericType in awaitableAdapterGenericTypes.Where(t => t.IsGenericParameter).Distinct())
            {
                ++openGenericArgumentsCount;
                if (!awaitableOpenGenericTypes.Remove(genericType))
                {
                    throw new ArgumentException($"awaitableAdapterType [{awaitableAdapterType.GetCorrectCSharpTypeName()}] has at least 1 open generic argument that is not contained in the awaitable type [{awaitableType.GetCorrectCSharpTypeName()}].");
                }
                builderOpenGenericTypes.Remove(genericType);
            }
            if (awaitableOpenGenericTypes.Count > 0)
            {
                throw new ArgumentException($"awaitable type [{awaitableType.GetCorrectCSharpTypeName()}] has at least 1 open generic argument that is not defined in awaitableAdapterType [{awaitableAdapterType.GetCorrectCSharpTypeName()}].");
            }
            if (builderOpenGenericTypes.Count > 0)
            {
                throw new ArgumentException($"asyncMethodBuilderAdapterType [{asyncMethodBuilderAdapterType.GetCorrectCSharpTypeName()}] has at least 1 open generic argument that is not defined in awaitableAdapterType [{awaitableAdapterType.GetCorrectCSharpTypeName()}].");
            }

            if (awaitableType.IsGenericType)
            {
                // Remap open generic types to their generic type definition (MyType<T, int> becomes MyType<TOriginal,int>)
                // Get generic types from the generic type definition, then fill in the non-generic types.
                var awaitableGenericTypeDefinition = awaitableType.GetGenericTypeDefinition();
                Type[] usableGenericTypes = awaitableGenericTypeDefinition.GetGenericArguments();
                for (int i = 0; i < usableGenericTypes.Length; ++i)
                {
                    if (!originalAwaitableGenericArguments[i].IsGenericParameter)
                    {
                        usableGenericTypes[i] = originalAwaitableGenericArguments[i];
                    }
                }
                AwaitableType = awaitableGenericTypeDefinition.MakeGenericType(usableGenericTypes);
            }
            else
            {
                AwaitableType = awaitableType;
            }
        }

        internal bool TryMatch(Type type, out ConcreteAsyncAdapter constructedAwaitableAdapter)
        {
            if (type == AwaitableType)
            {
                constructedAwaitableAdapter = new ConcreteAsyncAdapter()
                {
                    awaitableType = AwaitableType,
                    awaiterType = awaitableAdapterInterfaceGenericArguments[1],
                    awaitableAdapterType = AwaitableAdapterType,
                    asyncMethodBuilderAdapterType = AsyncMethodBuilderAdapterType,
                    resultType = awaitableAdapterInterfaceGenericArguments.Length == 2 ? null : awaitableAdapterInterfaceGenericArguments[2]
                };
                return true;
            }

            if (!type.IsGenericType || !AwaitableType.IsGenericType)
            {
                constructedAwaitableAdapter = null;
                return false;
            }
            if (type.GetGenericTypeDefinition() != AwaitableType.GetGenericTypeDefinition())
            {
                constructedAwaitableAdapter = null;
                return false;
            }

            // Match against closed generic types, and build a map of open generics to concrete types.
            Type[] actualGenericTypes = type.GetGenericArguments();
            var openToConcreteMap = new Dictionary<Type, Type>();
            for (int i = 0; i < actualGenericTypes.Length; ++i)
            {
                Type originalGenericArgument = originalAwaitableGenericArguments[i];
                if (originalGenericArgument.IsGenericParameter)
                {
                    openToConcreteMap[originalGenericArgument] = actualGenericTypes[i];
                    continue;
                }
                if (originalGenericArgument != actualGenericTypes[i])
                {
                    constructedAwaitableAdapter = null;
                    return false;
                }
            }

            // Construct the concrete types using the mapped open-to-concrete types.
            Type[] concreteAwaitableAdapterGenericArguments = MapToConcreteTypes(originalAwaitableGenericArguments, openToConcreteMap);
            Type concreteAwaitableAdapterType = AwaitableAdapterType.GetGenericTypeDefinition().MakeGenericType(concreteAwaitableAdapterGenericArguments);

            Type concreteAsyncMethodBuilderAdapterType = AsyncMethodBuilderAdapterType;
            if (AsyncMethodBuilderAdapterType.IsGenericType)
            {
                Type[] originalAsyncMethodBuilderAdapterGenericArguments = AsyncMethodBuilderAdapterType.GetGenericArguments();
                // Only construct the concrete asyncMethodBuilderAdapterType if it's not already concrete.
                if (originalAsyncMethodBuilderAdapterGenericArguments.Any(t => t.IsGenericParameter))
                {
                    Type[] concreteAsyncMethodBuilderAdapterGenericArguments = MapToConcreteTypes(originalAsyncMethodBuilderAdapterGenericArguments, openToConcreteMap);
                    concreteAsyncMethodBuilderAdapterType = AsyncMethodBuilderAdapterType.GetGenericTypeDefinition().MakeGenericType(concreteAwaitableAdapterGenericArguments);
                }
            }

            Type[] concreteAwaitableAdapterInterfaceGenericArguments = concreteAwaitableAdapterType.GetInterfaces()
                .Single(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IAwaitableAdapter<,>) || t.GetGenericTypeDefinition() == typeof(IAwaitableAdapter<,,>)))
                .GetGenericArguments();
            constructedAwaitableAdapter = new ConcreteAsyncAdapter()
            {
                awaitableType = type,
                awaiterType = concreteAwaitableAdapterInterfaceGenericArguments[1],
                awaitableAdapterType = concreteAwaitableAdapterType,
                asyncMethodBuilderAdapterType = concreteAsyncMethodBuilderAdapterType,
                resultType = concreteAwaitableAdapterInterfaceGenericArguments.Length == 2 ? null : concreteAwaitableAdapterInterfaceGenericArguments[2]
            };
            return true;
        }

        private static Type[] MapToConcreteTypes(Type[] originalTypes, Dictionary<Type, Type> openToConcreteMap)
        {
            Type[] concreteTypes = new Type[originalTypes.Length];
            originalTypes.CopyTo(concreteTypes, 0);
            for (int i = 0; i < concreteTypes.Length; ++i)
            {
                Type genericType = concreteTypes[i];
                if (genericType.IsGenericParameter)
                {
                    concreteTypes[i] = openToConcreteMap[genericType];
                }
            }
            return concreteTypes;
        }

        // We override the default reference type hashing and equality algorithms to forward to AwaitableAdapterType.
        // This makes AwaitableAdapterType the "key" when this type is added to a hashset.
        public override int GetHashCode()
            => AwaitableAdapterType.GetHashCode();

        public override bool Equals(object obj)
            => obj is AsyncAdapterDefinition def && Equals(def);

        public bool Equals(AsyncAdapterDefinition other)
            => AwaitableAdapterType.Equals(other.AwaitableAdapterType);

        // We compare against types with fewer open generic types first. This prioritizes exact matches over constructed matches (Task<int> is better than Task<>).
        public int CompareTo(AsyncAdapterDefinition other)
            => openGenericArgumentsCount.CompareTo(other.openGenericArgumentsCount);
    }

    internal sealed class ConcreteAsyncAdapter
    {
        internal Type awaitableType;
        internal Type awaiterType;
        internal Type awaitableAdapterType;
        internal Type asyncMethodBuilderAdapterType;
        internal Type? resultType;
    }
}