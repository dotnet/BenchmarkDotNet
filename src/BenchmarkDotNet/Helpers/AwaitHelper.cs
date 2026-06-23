using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Helpers;

[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
[AggressivelyOptimizeMethods]
public static class AwaitHelper
{
    private class ValueTaskWaiter
    {
        // We use thread static field so that each thread uses its own individual callback and reset event.
        [ThreadStatic]
        private static ValueTaskWaiter? ts_current;
        internal static ValueTaskWaiter Current => ts_current ??= new ValueTaskWaiter();

        // We cache the callback to prevent allocations for memory diagnoser.
        private readonly Action awaiterCallback;
        private readonly ManualResetEventSlim resetEvent;

        private ValueTaskWaiter()
        {
            resetEvent = new();
            awaiterCallback = resetEvent.Set;
        }

        internal void Wait<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
        {
            resetEvent.Reset();
            awaiter.UnsafeOnCompleted(awaiterCallback);

            // The fastest way to wait for completion is to spin a bit before waiting on the event. This is the same logic that Task.GetAwaiter().GetResult() uses.
            var spinner = new SpinWait();
            while (!resetEvent.IsSet)
            {
                if (spinner.NextSpinWillYield)
                {
                    resetEvent.Wait();
                    return;
                }
                spinner.SpinOnce();
            }
        }
    }

    // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way,
    // and will eventually throw actual exception, not aggregated one
    public static void GetResult(this Task task) => task.GetAwaiter().GetResult();

    public static T GetResult<T>(this Task<T> task) => task.GetAwaiter().GetResult();

    // ValueTask can be backed by an IValueTaskSource that only supports asynchronous awaits,
    // so we have to hook up a callback instead of calling .GetAwaiter().GetResult() like we do for Task.
    // The alternative is to convert it to Task using .AsTask(), but that causes allocations which we must avoid for memory diagnoser.
    public static void GetResult(this ValueTask task)
    {
        // Don't continue on the captured context, as that may result in a deadlock if the user runs this in-process.
        var awaiter = task.ConfigureAwait(false).GetAwaiter();
        if (!awaiter.IsCompleted)
        {
            ValueTaskWaiter.Current.Wait(awaiter);
        }
        awaiter.GetResult();
    }

    public static T GetResult<T>(this ValueTask<T> task)
    {
        // Don't continue on the captured context, as that may result in a deadlock if the user runs this in-process.
        var awaiter = task.ConfigureAwait(false).GetAwaiter();
        if (!awaiter.IsCompleted)
        {
            ValueTaskWaiter.Current.Wait(awaiter);
        }
        return awaiter.GetResult();
    }

    internal static MethodInfo? GetGetResultMethod(Type taskType)
    {
        if (!taskType.IsGenericType)
        {
            return typeof(AwaitHelper).GetMethod(nameof(GetResult), BindingFlags.Public | BindingFlags.Static, null, [taskType], null)!;
        }
        var genericTypeDefinition = taskType.GetGenericTypeDefinition();
        Type? compareType = genericTypeDefinition == typeof(ValueTask<>) ? typeof(ValueTask<>)
            : genericTypeDefinition == typeof(Task<>) ? typeof(Task<>)
            : null;
        if (compareType == null)
        {
            return null;
        }
        var resultType = taskType
            .GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!
            .ReturnType
            .GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)!
            .ReturnType;
        return typeof(AwaitHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m =>
            {
                if (m.Name != nameof(GetResult))
                    return false;
                Type paramType = m.GetParameters().First().ParameterType;
                return paramType.IsGenericType && paramType.GetGenericTypeDefinition() == compareType;
            })
            .MakeGenericMethod([resultType]);
    }

    internal static bool IsBuiltInTaskType(Type type)
    {
        if (!type.IsGenericType)
        {
            return type == typeof(ValueTask) || type == typeof(Task);
        }
        var genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(ValueTask<>)
            || genericTypeDefinition == typeof(Task<>);
    }

    internal static YieldAwaiter Yield() => default;

    public static ConfiguredTaskAwaiter ConfigureAwait(this Task task)
        => new(task);

    public static ConfiguredTaskAwaiter<TResult> ConfigureAwait<TResult>(this Task<TResult> task)
        => new(task);

    public static ConfiguredValueTaskAwaiter ConfigureAwait(this ValueTask task)
        => new(task);

    public static ConfiguredValueTaskAwaiter<TResult> ConfigureAwait<TResult>(this ValueTask<TResult> task)
        => new(task);

    internal static ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        => new(source, cancellationToken);

    internal readonly struct YieldAwaiter : ICriticalNotifyCompletion
    {
        public YieldAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                context.Post(continuation);
            }
            else
            {
                Task.Yield().GetAwaiter().OnCompleted(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                context.Post(continuation);
            }
            else
            {
                Task.Yield().GetAwaiter().UnsafeOnCompleted(continuation);
            }
        }
    }

    public readonly struct ConfiguredTaskAwaiter(Task task) : ICriticalNotifyCompletion
    {
        public ConfiguredTaskAwaiter GetAwaiter() => this;
        public bool IsCompleted => task.IsCompleted;
        public void GetResult() => task.GetAwaiter().GetResult();

        public void OnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                task.ConfigureAwait(false).GetAwaiter().OnCompleted(context.GetPassthroughContinuation(continuation));
            }
            else
            {
                task.ConfigureAwait(true).GetAwaiter().OnCompleted(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(context.GetPassthroughContinuation(continuation));
            }
            else
            {
                task.ConfigureAwait(true).GetAwaiter().UnsafeOnCompleted(continuation);
            }
        }
    }

    public readonly struct ConfiguredTaskAwaiter<TResult>(Task<TResult> task) : ICriticalNotifyCompletion
    {
        public ConfiguredTaskAwaiter<TResult> GetAwaiter() => this;
        public bool IsCompleted => task.IsCompleted;
        public TResult GetResult() => task.GetAwaiter().GetResult();

        public void OnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                task.ConfigureAwait(false).GetAwaiter().OnCompleted(context.GetPassthroughContinuation(continuation));
            }
            else
            {
                task.ConfigureAwait(true).GetAwaiter().OnCompleted(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(context.GetPassthroughContinuation(continuation));
            }
            else
            {
                task.ConfigureAwait(true).GetAwaiter().UnsafeOnCompleted(continuation);
            }
        }
    }

    public readonly struct ConfiguredValueTaskAwaiter(ValueTask valueTask) : ICriticalNotifyCompletion
    {
        private readonly ValueTask _valueTask = valueTask;
        public ConfiguredValueTaskAwaiter GetAwaiter() => this;
        public bool IsCompleted => _valueTask.IsCompleted;
        public void GetResult() => _valueTask.GetAwaiter().GetResult();

        public void OnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                _valueTask.ConfigureAwait(false).GetAwaiter().OnCompleted(context.GetPassthroughContinuation(continuation));
            }
            else
            {
                _valueTask.ConfigureAwait(true).GetAwaiter().OnCompleted(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                _valueTask.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(context.GetPassthroughContinuation(continuation));
            }
            else
            {
                _valueTask.ConfigureAwait(true).GetAwaiter().UnsafeOnCompleted(continuation);
            }
        }
    }

    public readonly struct ConfiguredValueTaskAwaiter<TResult>(ValueTask<TResult> valueTask) : ICriticalNotifyCompletion
    {
        private readonly ValueTask<TResult> _valueTask = valueTask;
        public ConfiguredValueTaskAwaiter<TResult> GetAwaiter() => this;
        public bool IsCompleted => _valueTask.IsCompleted;
        public TResult GetResult() => _valueTask.GetAwaiter().GetResult();

        public void OnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                _valueTask.ConfigureAwait(false).GetAwaiter().OnCompleted(context.GetPassthroughContinuation(continuation));
            }
            else
            {
                _valueTask.ConfigureAwait(true).GetAwaiter().OnCompleted(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (BenchmarkSynchronizationContext.Current is { } context)
            {
                _valueTask.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(context.GetPassthroughContinuation(continuation));
            }
            else
            {
                _valueTask.ConfigureAwait(true).GetAwaiter().UnsafeOnCompleted(continuation);
            }
        }
    }

    internal readonly struct ConfiguredCancelableAsyncEnumerable<T>
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    {
        private readonly IAsyncEnumerable<T> _enumerable;
        private readonly CancellationToken _cancellationToken;

        internal ConfiguredCancelableAsyncEnumerable(IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            _enumerable = enumerable;
            _cancellationToken = cancellationToken;
        }

        public Enumerator GetAsyncEnumerator() =>
            new(_enumerable.GetAsyncEnumerator(_cancellationToken));

        public readonly struct Enumerator
        {
            private readonly IAsyncEnumerator<T> _enumerator;

            internal Enumerator(IAsyncEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public ConfiguredValueTaskAwaiter<bool> MoveNextAsync() =>
                _enumerator.MoveNextAsync().ConfigureAwait();

            public T Current => _enumerator.Current;

            public ConfiguredValueTaskAwaiter DisposeAsync() =>
                _enumerator.DisposeAsync().ConfigureAwait();
        }
    }
}