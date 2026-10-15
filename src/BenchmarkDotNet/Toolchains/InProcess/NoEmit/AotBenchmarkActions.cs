using BenchmarkDotNet.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

/// <summary>
/// Creates benchmark actions without <see cref="Type.MakeGenericType"/>, for runtimes that cannot generate code
/// (Native AOT). There a generic action can only be created for an instantiation that was compiled ahead of time,
/// so every action created here is a closed type referenced in this file:
/// reference types share <see cref="BenchmarkAction{T}"/> of <see cref="object"/> (a method returning a class binds
/// to <c>Func&lt;object&gt;</c>), <see cref="Task{TResult}"/> is awaited as a <see cref="Task"/>, and common value types
/// have their own instantiations. Other types get theirs through <see cref="Register{T}"/>, which BenchmarkDotNet's
/// source generator calls for the return types of the benchmarks in a project published with Native AOT.
/// </summary>
internal static class AotBenchmarkActions
{
    private static readonly IFactory ReferenceTypes = new Factory<object>();

    // Keyed by the exact type: the value types below, plus whatever was registered.
    private static readonly ConcurrentDictionary<Type, IFactory> Factories = new(new Dictionary<Type, IFactory>
    {
        [typeof(bool)] = new Factory<bool>(),
        [typeof(byte)] = new Factory<byte>(),
        [typeof(sbyte)] = new Factory<sbyte>(),
        [typeof(char)] = new Factory<char>(),
        [typeof(short)] = new Factory<short>(),
        [typeof(ushort)] = new Factory<ushort>(),
        [typeof(int)] = new Factory<int>(),
        [typeof(uint)] = new Factory<uint>(),
        [typeof(long)] = new Factory<long>(),
        [typeof(ulong)] = new Factory<ulong>(),
        [typeof(float)] = new Factory<float>(),
        [typeof(double)] = new Factory<double>(),
        [typeof(decimal)] = new Factory<decimal>(),
        [typeof(IntPtr)] = new Factory<IntPtr>(),
        [typeof(UIntPtr)] = new Factory<UIntPtr>(),
        [typeof(DateTime)] = new Factory<DateTime>(),
        [typeof(TimeSpan)] = new Factory<TimeSpan>(),
        [typeof(Guid)] = new Factory<Guid>(),
    });

    /// <summary>
    /// Makes actions available for methods returning <typeparamref name="T"/>, <c>ref T</c>, <see cref="ValueTask{TResult}"/>
    /// of <typeparamref name="T"/> or <see cref="IAsyncEnumerable{T}"/> of <typeparamref name="T"/>: calling this with a
    /// concrete type is what makes the compiler generate their native code.
    /// </summary>
    internal static void Register<T>() => Factories.TryAdd(typeof(T), new Factory<T>());

    /// <summary>
    /// Whether generic actions have to be created here: only where code cannot be generated. Elsewhere (the JIT, or an
    /// interpreter such as Mono's on WebAssembly) <see cref="BenchmarkActionFactory"/> creates them as before.
    /// </summary>
    internal static bool IsRequired
#if NETSTANDARD2_0
        => false; // Native AOT applications use the .NET 8+ build
#else
        => !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;
#endif

    /// <summary>Whether <see cref="TryCreate"/> can create an action for a method with this return type.</summary>
    internal static bool IsSupported(Type resultType) => GetShape(resultType, out _) != Shape.None;

    internal static bool TryCreate(object? instance, MethodInfo method, Type resultType, int unrollFactor, bool consumeTasksSynchronously,
        [NotNullWhen(true)] out IBenchmarkAction? action)
    {
        action = GetShape(resultType, out var factory) switch
        {
            Shape.Task => consumeTasksSynchronously
                ? new BenchmarkActionBlockingTask(instance, method, unrollFactor)
                : new BenchmarkActionTask(instance, method, unrollFactor),
            Shape.ValueTask => factory!.ValueTask(instance, method, unrollFactor, consumeTasksSynchronously),
            Shape.AsyncEnumerable => factory!.AsyncEnumerable(instance, method, unrollFactor),
            Shape.ByRef => IsReadOnly(method.ReturnParameter)
                ? factory!.ByRefReadonly(instance, method, unrollFactor)
                : factory!.ByRef(instance, method, unrollFactor),
            Shape.Value => factory!.Value(instance, method, unrollFactor),
            _ => null,
        };
        return action != null;
    }

    // IsReadOnlyAttribute is not part of netstandard2.0, so it is matched by name, as in BenchmarkActionFactory.
    private static bool IsReadOnly(ParameterInfo returnParameter)
        => returnParameter.GetCustomAttributes().Any(attribute => attribute.GetType().FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");

    // Mirrors the order of BenchmarkActionFactory.CreateCore, so a type it rejects (a custom awaitable, say) is not accepted here.
    private static Shape GetShape(Type resultType, out IFactory? factory)
    {
        factory = null;

        if (resultType.IsPointer || resultType == typeof(void))
            return Shape.None;

        // A ref return cannot bind to a delegate returning a ref of another type, so even a class needs its own instantiation.
        if (resultType.IsByRef)
            return Factories.TryGetValue(resultType.GetElementType()!, out factory) ? Shape.ByRef : Shape.None;

        if (resultType.IsGenericType)
        {
            var definition = resultType.GetGenericTypeDefinition();
            var argument = resultType.GenericTypeArguments[0];

            if (definition == typeof(Task<>))
                return Shape.Task;

            // ValueTask<T> is a struct, so ValueTask<string> does not convert to ValueTask<object> either.
            if (definition == typeof(ValueTask<>))
                return Factories.TryGetValue(argument, out factory) ? Shape.ValueTask : Shape.None;

            if (definition == typeof(IAsyncEnumerable<>))
            {
                factory = GetFactory(argument);
                return factory != null ? Shape.AsyncEnumerable : Shape.None;
            }
        }

        if (resultType.IsAwaitable(out _) || resultType.IsAsyncEnumerable(out _))
            return Shape.None;

        factory = GetFactory(resultType);
        return factory != null ? Shape.Value : Shape.None;
    }

    // Reference types share the object instantiation, which a method returning a class (or IAsyncEnumerable of one) binds to.
    private static IFactory? GetFactory(Type type)
        => !type.IsValueType ? ReferenceTypes
        : Factories.TryGetValue(type, out var factory) ? factory
        : null;

    private enum Shape { None, Task, ValueTask, AsyncEnumerable, ByRef, Value }

    private interface IFactory
    {
        BenchmarkActionBase Value(object? instance, MethodInfo method, int unrollFactor);
        BenchmarkActionBase ValueTask(object? instance, MethodInfo method, int unrollFactor, bool blocking);
        BenchmarkActionBase AsyncEnumerable(object? instance, MethodInfo method, int unrollFactor);
        BenchmarkActionBase ByRef(object? instance, MethodInfo method, int unrollFactor);
        BenchmarkActionBase ByRefReadonly(object? instance, MethodInfo method, int unrollFactor);
    }

    private sealed class Factory<T> : IFactory
    {
        public BenchmarkActionBase Value(object? instance, MethodInfo method, int unrollFactor)
            => new BenchmarkAction<T>(instance, method, unrollFactor);

        public BenchmarkActionBase ValueTask(object? instance, MethodInfo method, int unrollFactor, bool blocking)
            => blocking
                ? new BenchmarkActionBlockingValueTask<T>(instance, method, unrollFactor)
                : new BenchmarkActionValueTask<T>(instance, method, unrollFactor);

        public BenchmarkActionBase AsyncEnumerable(object? instance, MethodInfo method, int unrollFactor)
            => new BenchmarkActionAsyncEnumerable<T>(instance, method, unrollFactor);

        public BenchmarkActionBase ByRef(object? instance, MethodInfo method, int unrollFactor)
            => new BenchmarkActionByRef<T>(instance, method, unrollFactor);

        public BenchmarkActionBase ByRefReadonly(object? instance, MethodInfo method, int unrollFactor)
            => new BenchmarkActionByRefReadonly<T>(instance, method, unrollFactor);
    }
}
