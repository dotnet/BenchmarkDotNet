using BenchmarkDotNet.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

/// <summary>
/// Creates benchmark actions without <see cref="Type.MakeGenericType"/>, for runtimes that cannot generate code
/// (Native AOT). There a generic action can only be created for an instantiation that was compiled ahead of time,
/// so every action created here is a closed type referenced in this file:
/// reference types share <see cref="BenchmarkAction{T}"/> of <see cref="object"/> (a method returning a class binds
/// to <c>Func&lt;object&gt;</c>), <see cref="Task{TResult}"/> is awaited as a <see cref="Task"/>, and common value types
/// have their own instantiations.
/// </summary>
internal static class AotBenchmarkActions
{
    private static readonly IFactory ReferenceTypes = new Factory<object>();

    private static readonly Dictionary<Type, IFactory> ValueTypes = new()
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
    };

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
            Shape.Value => factory!.Value(instance, method, unrollFactor),
            _ => null,
        };
        return action != null;
    }

    // Mirrors the order of BenchmarkActionFactory.CreateCore, so a type it rejects (a custom awaitable, say) is not accepted here.
    private static Shape GetShape(Type resultType, out IFactory? factory)
    {
        factory = null;

        if (resultType.IsByRef || resultType.IsPointer || resultType == typeof(void))
            return Shape.None;

        if (resultType.IsGenericType)
        {
            var definition = resultType.GetGenericTypeDefinition();
            var argument = resultType.GenericTypeArguments[0];

            if (definition == typeof(Task<>))
                return Shape.Task;

            if (definition == typeof(ValueTask<>))
                return ValueTypes.TryGetValue(argument, out factory) ? Shape.ValueTask : Shape.None;

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

    private static IFactory? GetFactory(Type type)
        => !type.IsValueType ? ReferenceTypes
        : ValueTypes.TryGetValue(type, out var factory) ? factory
        : null;

    private enum Shape { None, Task, ValueTask, AsyncEnumerable, Value }

    private interface IFactory
    {
        BenchmarkActionBase Value(object? instance, MethodInfo method, int unrollFactor);
        BenchmarkActionBase ValueTask(object? instance, MethodInfo method, int unrollFactor, bool blocking);
        BenchmarkActionBase AsyncEnumerable(object? instance, MethodInfo method, int unrollFactor);
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
    }
}
