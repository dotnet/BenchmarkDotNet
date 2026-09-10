using BenchmarkDotNet.Extensions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Helpers;

internal static class DynamicAwaitHelper
{
    internal static async ValueTask<(bool hasResult, object? result)> AwaitResult(object value, AwaitableInfo awaitableInfo)
    {
        var result = await new DynamicAwaitable(awaitableInfo, value);
        return (awaitableInfo.ResultType != typeof(void), result);
    }

    internal static IAsyncEnumerable<object?> EnumerateSourceAsync(object asyncEnumerable, Type elementType)
        // Sources are always read through IAsyncEnumerable<T>, pattern-based await foreach types are not supported.
        // A reference-type element needs no reflection at all: IAsyncEnumerable<out T> is covariant.
        => !elementType.IsValueType
            ? (IAsyncEnumerable<object?>) asyncEnumerable
            : EnumerateSourceAsyncCore(asyncEnumerable, ReflectionExtensions.GetAsyncEnumerableInterfaceInfo(elementType));

    private static async IAsyncEnumerable<object?> EnumerateSourceAsyncCore(object asyncEnumerable, AsyncEnumerableInfo asyncEnumerableInfo, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var enumerator = Unwrapped(() => asyncEnumerableInfo.GetAsyncEnumeratorMethod.Invoke(asyncEnumerable, [cancellationToken]))!;
        var moveNextAsyncArgs = GetDefaultArgs(asyncEnumerableInfo.MoveNextAsyncMethod);

        try
        {
            while (await ((ValueTask<bool>) Unwrapped(() => asyncEnumerableInfo.MoveNextAsyncMethod.Invoke(enumerator, moveNextAsyncArgs))!).ConfigureAwait(false))
            {
                yield return Unwrapped(() => asyncEnumerableInfo.CurrentProperty.GetValue(enumerator));
            }
        }
        finally
        {
            await ((IAsyncDisposable) enumerator).DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async IAsyncEnumerable<object?> EnumerateBenchmarkAsync(object asyncEnumerable, AsyncEnumerableInfo asyncEnumerableInfo)
    {
        var enumerator = Unwrapped(() => asyncEnumerableInfo.GetAsyncEnumeratorMethod.Invoke(asyncEnumerable, GetDefaultArgs(asyncEnumerableInfo.GetAsyncEnumeratorMethod)))!;

        var moveNextAsyncArgs = GetDefaultArgs(asyncEnumerableInfo.MoveNextAsyncMethod);
        var currentProperty = asyncEnumerableInfo.CurrentProperty;
        var moveNextAwaitable = asyncEnumerableInfo.MoveNextAwaitable;

        // DisposeAsync is optional in the await-foreach pattern: Roslyn matches a public instance DisposeAsync
        // whose parameters are all optional and whose return type is awaitable with a void GetResult, else the
        // IAsyncDisposable interface.
        MethodInfo? disposeAsyncMethod = null;
        AwaitableInfo? disposeAwaitableInfo = null;
        foreach (var candidate in asyncEnumerableInfo.EnumeratorType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (candidate.Name == nameof(IAsyncDisposable.DisposeAsync)
                && candidate.GetParameters().All(p => p.IsOptional)
                && candidate.ReturnType.IsAwaitable(out var awaitable)
                && awaitable.ResultType == typeof(void))
            {
                disposeAsyncMethod = candidate;
                disposeAwaitableInfo = awaitable;
                break;
            }
        }
        if (disposeAsyncMethod is null && typeof(IAsyncDisposable).IsAssignableFrom(asyncEnumerableInfo.EnumeratorType))
        {
            disposeAsyncMethod = typeof(IAsyncDisposable).GetMethod(nameof(IAsyncDisposable.DisposeAsync))!;
            disposeAsyncMethod.ReturnType.IsAwaitable(out disposeAwaitableInfo);
        }
        var disposeAsyncArgs = disposeAsyncMethod is null ? null : GetDefaultArgs(disposeAsyncMethod);

        try
        {
            while (true)
            {
                var moveNextResult = Unwrapped(() => asyncEnumerableInfo.MoveNextAsyncMethod.Invoke(enumerator, moveNextAsyncArgs));
                bool hasMore = (bool)(await new DynamicAwaitable(moveNextAwaitable, moveNextResult!))!;
                if (!hasMore)
                {
                    break;
                }
                yield return Unwrapped(() => currentProperty.GetValue(enumerator));
            }
        }
        finally
        {
            if (disposeAsyncMethod != null)
            {
                var disposeResult = Unwrapped(() => disposeAsyncMethod.Invoke(enumerator, disposeAsyncArgs));
                if (disposeResult != null)
                {
                    await new DynamicAwaitable(disposeAwaitableInfo!, disposeResult);
                }
            }
        }
    }

    private static object?[] GetDefaultArgs(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return [];
        }
        var args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = parameters[i].GetDefaultArgumentValue();
        }
        return args;
    }

    private readonly struct DynamicAwaitable(AwaitableInfo awaitableInfo, object awaitable)
    {
        public DynamicAwaiter GetAwaiter()
        {
            // Read into locals: a lambda in a struct's instance member cannot capture a primary constructor parameter.
            var info = awaitableInfo;
            object target = awaitable;

            return new(info, Unwrapped(() => info.GetAwaiterMethod.Invoke(target, null)));
        }
    }

    // Reflection wraps exceptions in TargetInvocationException, while the covariance path lets it through as thrown.
    // Here we unwrap it and rethrow while preserving its stacktrace so both paths exceptions behave consistently.
    private static object? Unwrapped(Func<object?> invoke)
    {
        try
        {
            return invoke();
        }
        catch (TargetInvocationException exception) when (exception.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw; // Not reached: Throw() always throws.
        }
    }

    private readonly struct DynamicAwaiter(AwaitableInfo awaitableInfo, object? awaiter) : ICriticalNotifyCompletion
    {
        public bool IsCompleted
        {
            get
            {
                var isCompleted = awaitableInfo.IsCompletedProperty.GetMethod!;
                object? target = awaiter;

                return Unwrapped(() => isCompleted.Invoke(target, null)) is true;
            }
        }

        public object? GetResult()
        {
            // Read into locals: a lambda in a struct's instance member cannot capture a primary constructor parameter.
            var getResult = awaitableInfo.GetResultMethod;
            object? target = awaiter;

            return Unwrapped(() => getResult.Invoke(target, null));
        }

        public void OnCompleted(Action continuation)
            => OnCompletedCore(typeof(INotifyCompletion), nameof(INotifyCompletion.OnCompleted), continuation);

        public void UnsafeOnCompleted(Action continuation)
            => OnCompletedCore(typeof(ICriticalNotifyCompletion), nameof(ICriticalNotifyCompletion.UnsafeOnCompleted), continuation);

        private void OnCompletedCore(Type interfaceType, string methodName, Action continuation)
        {
            // ICriticalNotifyCompletion is optional in the awaiter pattern, but DynamicAwaiter declares it, so a
            // state machine awaiting one always takes UnsafeOnCompleted. Asking GetInterfaceMap for an interface the
            // user's awaiter does not implement throws, so hand those to OnCompleted - which flows the execution
            // context that UnsafeOnCompleted exists to skip, the safe direction.
            if (interfaceType == typeof(ICriticalNotifyCompletion)
                && !typeof(ICriticalNotifyCompletion).IsAssignableFrom(awaitableInfo.AwaiterType))
            {
                OnCompletedCore(typeof(INotifyCompletion), nameof(INotifyCompletion.OnCompleted), continuation);
                return;
            }

            var onCompletedMethod = interfaceType.GetMethod(methodName)!;

            // The awaiter pattern binds on the awaiter's declared type, which may itself be an interface -
            // GetInterfaceMap refuses one ("'this' type cannot be an interface itself"), and the throw lands on
            // AwaitUnsafeOnCompleted, where it is rethrown onto the thread pool and takes the process down. No map
            // is needed here anyway: invoking the interface method dispatches to whatever implements it, an
            // explicit implementation included.
            if (awaitableInfo.AwaiterType.IsInterface)
            {
                object? interfaceTarget = awaiter;
                Unwrapped(() => onCompletedMethod.Invoke(interfaceTarget, [continuation]));
                return;
            }

            var map = awaitableInfo.AwaiterType.GetInterfaceMap(interfaceType);

            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i] == onCompletedMethod)
                {
                    var onCompleted = map.TargetMethods[i];
                    object? target = awaiter;

                    Unwrapped(() => onCompleted.Invoke(target, [continuation]));
                    return;
                }
            }
        }
    }
}
