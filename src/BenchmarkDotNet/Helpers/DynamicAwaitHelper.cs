using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Helpers;

internal static class DynamicAwaitHelper
{
    internal static async ValueTask<(bool hasResult, object? result)> AwaitResult(object value, Type declaredType)
    {
        var getAwaiterMethod = declaredType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!;
        var awaiterType = getAwaiterMethod.ReturnType;
        var getResultMethod = awaiterType.GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)!;
        var result = await new DynamicAwaitable(getAwaiterMethod, awaiterType, getResultMethod, value);
        return (getResultMethod.ReturnType != typeof(void), result);
    }

    internal static ValueTask DrainAsyncEnumerableAsync(object asyncEnumerable, Type declaredType)
        => EnumerateCoreAsync(asyncEnumerable, declaredType, items: null);

    internal static async ValueTask<List<object?>> ToListAsync(object asyncEnumerable, Type declaredType)
    {
        List<object?> items = [];
        await EnumerateCoreAsync(asyncEnumerable, declaredType, items).ConfigureAwait(false);
        return items;
    }

    private static async ValueTask EnumerateCoreAsync(object asyncEnumerable, Type declaredType, List<object?>? items)
    {
        var (getAsyncEnumeratorMethod, getAsyncEnumeratorArgs) = ResolveGetAsyncEnumerator(declaredType);
        var enumerator = getAsyncEnumeratorMethod.Invoke(asyncEnumerable, getAsyncEnumeratorArgs)!;

        // Look up enumerator members via GetAsyncEnumerator's declared return type rather than the runtime
        // type. For the interface path that's IAsyncEnumerator<T>, whose interface methods dispatch virtually
        // — important for compiler-generated async iterator state machines that implement MoveNextAsync /
        // Current as explicit interface members and so don't surface them as public instance members on the
        // runtime type. For the pattern path it's the concrete enumerator type with public members.
        var enumeratorMemberType = getAsyncEnumeratorMethod.ReturnType;

        var moveNextAsyncMethod = enumeratorMemberType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == nameof(IAsyncEnumerator<>.MoveNextAsync) && m.GetParameters().All(p => p.IsOptional))
            ?? throw new InvalidOperationException($"Type {enumeratorMemberType} does not expose a MoveNextAsync method.");
        var moveNextAsyncArgs = GetDefaultArgs(moveNextAsyncMethod);
        var currentProperty = enumeratorMemberType.GetProperty(nameof(IAsyncEnumerator<>.Current), BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Type {enumeratorMemberType} does not expose a Current property.");
        // DisposeAsync is optional for the pattern. Prefer a public instance method on the declared enumerator
        // type with all-optional parameters whose awaiter's GetResult returns void; otherwise fall back to the
        // IAsyncDisposable interface implementation.
        var disposeAsyncMethod = enumeratorMemberType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(IAsyncDisposable.DisposeAsync)
                    && m.GetParameters().All(p => p.IsOptional)
                    && m.ReturnType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)
                        ?.ReturnType
                        .GetMethod(nameof(System.Runtime.CompilerServices.TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)
                        ?.ReturnType == typeof(void))
            ?? (typeof(IAsyncDisposable).IsAssignableFrom(enumeratorMemberType)
                ? typeof(IAsyncDisposable).GetMethod(nameof(IAsyncDisposable.DisposeAsync))
                : null);
        var disposeAsyncArgs = disposeAsyncMethod is null ? null : GetDefaultArgs(disposeAsyncMethod);

        try
        {
            while (true)
            {
                var moveNextResult = moveNextAsyncMethod.Invoke(enumerator, moveNextAsyncArgs);
                bool hasMore = (bool)(await AwaitDynamicAsync(moveNextAsyncMethod.ReturnType, moveNextResult!).ConfigureAwait(false))!;
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
                    await AwaitDynamicAsync(disposeAsyncMethod.ReturnType, disposeResult).ConfigureAwait(false);
                }
            }
        }
    }

    private static (MethodInfo method, object?[] args) ResolveGetAsyncEnumerator(Type enumerableType)
    {
        // Mirror IsAsyncEnumerable's precedence: exact IAsyncEnumerable<T>, then pattern, then interface fallback.
        if (enumerableType.IsGenericType && enumerableType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            var method = enumerableType.GetMethod(nameof(IAsyncEnumerable<>.GetAsyncEnumerator))!;
            return (method, [CancellationToken.None]);
        }
        var pattern = enumerableType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == nameof(IAsyncEnumerable<>.GetAsyncEnumerator) && m.GetParameters().All(p => p.IsOptional));
        if (pattern != null)
        {
            return (pattern, GetDefaultArgs(pattern));
        }
        foreach (var iface in enumerableType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                var method = iface.GetMethod(nameof(IAsyncEnumerable<>.GetAsyncEnumerator))!;
                return (method, [CancellationToken.None]);
            }
        }
        throw new InvalidOperationException($"Type {enumerableType} is not an async enumerable.");
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

    private static async ValueTask<object?> AwaitDynamicAsync(Type awaitableType, object awaitable)
    {
        var getAwaiterMethod = awaitableType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!;
        var awaiterType = getAwaiterMethod.ReturnType;
        var getResultMethod = awaiterType.GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)!;
        return await new DynamicAwaitable(getAwaiterMethod, awaiterType, getResultMethod, awaitable);
    }

    private readonly struct DynamicAwaitable(MethodInfo getAwaiterMethod, Type awaiterType, MethodInfo getResultMethod, object awaitable)
    {
        public DynamicAwaiter GetAwaiter()
            => new(awaiterType, getResultMethod, getAwaiterMethod.Invoke(awaitable, null));
    }

    private readonly struct DynamicAwaiter(Type awaiterType, MethodInfo getResultMethod, object? awaiter) : ICriticalNotifyCompletion
    {
        public bool IsCompleted
            => awaiterType.GetProperty(nameof(TaskAwaiter.IsCompleted), BindingFlags.Public | BindingFlags.Instance)!.GetMethod!.Invoke(awaiter, null) is true;

        public object? GetResult()
            => getResultMethod.Invoke(awaiter, null);

        public void OnCompleted(Action continuation)
            => OnCompletedCore(typeof(INotifyCompletion), nameof(INotifyCompletion.OnCompleted), continuation);

        public void UnsafeOnCompleted(Action continuation)
            => OnCompletedCore(typeof(ICriticalNotifyCompletion), nameof(ICriticalNotifyCompletion.UnsafeOnCompleted), continuation);

        private void OnCompletedCore(Type interfaceType, string methodName, Action continuation)
        {
            var onCompletedMethod = interfaceType.GetMethod(methodName);
            var map = awaiterType.GetInterfaceMap(interfaceType);

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
