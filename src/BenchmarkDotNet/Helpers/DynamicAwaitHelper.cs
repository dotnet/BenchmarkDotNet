using BenchmarkDotNet.Extensions;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Helpers;

internal static class DynamicAwaitHelper
{
    internal static async ValueTask<(bool hasResult, object? result)> GetOrAwaitResult(object? value)
    {
        var valueType = value?.GetType();
        if (valueType?.IsAwaitable() != true)
        {
            return (true, value);
        }

        var getAwaiterMethod = valueType.GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!;
        var awaiterType = getAwaiterMethod.ReturnType;
        var getResultMethod = awaiterType.GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)!;
        var result = await new DynamicAwaitable(getAwaiterMethod, awaiterType, getResultMethod, value!);
        return (getResultMethod.ReturnType != typeof(void), result);
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
