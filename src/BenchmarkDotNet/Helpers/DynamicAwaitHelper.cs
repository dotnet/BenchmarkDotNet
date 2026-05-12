using BenchmarkDotNet.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Helpers;

internal static class DynamicAwaitHelper
{
    internal static async ValueTask<(bool hasResult, object? result)> AwaitResult(object value, AwaitableInfo awaitableInfo)
    {
        var result = await new DynamicAwaitable(awaitableInfo, value);
        return (awaitableInfo.ResultType != typeof(void), result);
    }

    internal static ValueTask DrainAsyncEnumerableAsync(object asyncEnumerable, AsyncEnumerableInfo enumerableInfo)
        => EnumerateCoreAsync(asyncEnumerable, enumerableInfo, items: null);

    internal static async ValueTask<List<object?>> ToListAsync(object asyncEnumerable, AsyncEnumerableInfo enumerableInfo)
    {
        List<object?> items = [];
        await EnumerateCoreAsync(asyncEnumerable, enumerableInfo, items).ConfigureAwait(false);
        return items;
    }

    private static async ValueTask EnumerateCoreAsync(object asyncEnumerable, AsyncEnumerableInfo enumerableInfo, List<object?>? items)
    {
        var getAsyncEnumeratorArgs = GetDefaultArgs(enumerableInfo.GetAsyncEnumeratorMethod);
        var enumerator = enumerableInfo.GetAsyncEnumeratorMethod.Invoke(asyncEnumerable, getAsyncEnumeratorArgs)!;

        var moveNextAsyncArgs = GetDefaultArgs(enumerableInfo.MoveNextAsyncMethod);
        var currentProperty = enumerableInfo.CurrentProperty;
        var moveNextAwaitable = enumerableInfo.MoveNextAwaitable;

        // DisposeAsync is optional for the await-foreach pattern. Roslyn matches a public instance
        // method named DisposeAsync whose parameters are all optional and whose return type satisfies
        // the awaitable pattern with a void GetResult; otherwise it falls back to the IAsyncDisposable
        // interface dispatch.
        MethodInfo? disposeAsyncMethod = null;
        AwaitableInfo? disposeAwaitableInfo = null;
        foreach (var candidate in enumerableInfo.EnumeratorType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
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
        if (disposeAsyncMethod is null && typeof(IAsyncDisposable).IsAssignableFrom(enumerableInfo.EnumeratorType))
        {
            disposeAsyncMethod = typeof(IAsyncDisposable).GetMethod(nameof(IAsyncDisposable.DisposeAsync))!;
            disposeAsyncMethod.ReturnType.IsAwaitable(out disposeAwaitableInfo);
        }
        var disposeAsyncArgs = disposeAsyncMethod is null ? null : GetDefaultArgs(disposeAsyncMethod);

        try
        {
            while (true)
            {
                var moveNextResult = enumerableInfo.MoveNextAsyncMethod.Invoke(enumerator, moveNextAsyncArgs);
                bool hasMore = (bool)(await new DynamicAwaitable(moveNextAwaitable, moveNextResult!))!;
                if (!hasMore)
                {
                    break;
                }
                items?.Add(currentProperty.GetValue(enumerator));
            }
        }
        finally
        {
            if (disposeAsyncMethod != null)
            {
                var disposeResult = disposeAsyncMethod.Invoke(enumerator, disposeAsyncArgs);
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
            args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
        }
        return args;
    }

    private readonly struct DynamicAwaitable(AwaitableInfo awaitableInfo, object awaitable)
    {
        public DynamicAwaiter GetAwaiter()
            => new(awaitableInfo, awaitableInfo.GetAwaiterMethod.Invoke(awaitable, null));
    }

    private readonly struct DynamicAwaiter(AwaitableInfo awaitableInfo, object? awaiter) : ICriticalNotifyCompletion
    {
        public bool IsCompleted
            => awaitableInfo.IsCompletedProperty.GetMethod!.Invoke(awaiter, null) is true;

        public object? GetResult()
            => awaitableInfo.GetResultMethod.Invoke(awaiter, null);

        public void OnCompleted(Action continuation)
            => OnCompletedCore(typeof(INotifyCompletion), nameof(INotifyCompletion.OnCompleted), continuation);

        public void UnsafeOnCompleted(Action continuation)
            => OnCompletedCore(typeof(ICriticalNotifyCompletion), nameof(ICriticalNotifyCompletion.UnsafeOnCompleted), continuation);

        private void OnCompletedCore(Type interfaceType, string methodName, Action continuation)
        {
            var onCompletedMethod = interfaceType.GetMethod(methodName);
            var map = awaitableInfo.AwaiterType.GetInterfaceMap(interfaceType);

            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i] == onCompletedMethod)
                {
                    map.TargetMethods[i].Invoke(awaiter, [continuation]);
                    return;
                }
            }
        }
    }
}
