using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Running
{
    /// <summary>
    /// Creates instances of benchmark types. In a Native AOT application, a generic benchmark type closed over a value type
    /// (from <c>[GenericTypeArguments]</c>) has no native code unless something constructs it, so the source-generated
    /// registration (<see cref="NativeAotRegistration.RegisterBenchmarkType{T}"/>) supplies a factory that does.
    /// </summary>
    internal static class BenchmarkInstanceFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<object>> Factories = new();

        internal static void Register(Type type, Func<object> factory) => Factories.TryAdd(type, factory);

        internal static object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
            => Factories.TryGetValue(type, out var factory) ? factory() : Activator.CreateInstance(type)!;
    }
}
