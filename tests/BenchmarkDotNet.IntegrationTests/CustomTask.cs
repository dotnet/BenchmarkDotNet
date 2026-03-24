using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;

namespace BenchmarkDotNet.IntegrationTests;

[AsyncMethodBuilder(typeof(AsyncCustomTaskMethodBuilder))]
public readonly struct CustomTask(ValueTask valueTask)
{
    public CustomTaskAwaiter GetAwaiter() => new(valueTask.GetAwaiter());
}

public readonly struct CustomTaskAwaiter(ValueTaskAwaiter valueTaskAwaiter) : ICriticalNotifyCompletion
{
    public bool IsCompleted => valueTaskAwaiter.IsCompleted;

    public void GetResult() => valueTaskAwaiter.GetResult();

    public void OnCompleted(Action continuation) => valueTaskAwaiter.OnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation) => valueTaskAwaiter.UnsafeOnCompleted(continuation);
}

public struct AsyncCustomTaskMethodBuilder
{
    public static int InUseCounter { get; private set; }

    private AsyncValueTaskMethodBuilder _methodBuilder;

    public static AsyncCustomTaskMethodBuilder Create()
    {
        ++InUseCounter;
        return default;
    }

    public CustomTask Task => new(_methodBuilder.Task);

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        => _methodBuilder.Start(ref stateMachine);

    public void SetStateMachine(IAsyncStateMachine stateMachine) => _methodBuilder.SetStateMachine(stateMachine);

    public void SetResult()
    {
        --InUseCounter;
        _methodBuilder.SetResult();
    }

    public void SetException(Exception exception)
    {
        --InUseCounter;
        _methodBuilder.SetException(exception);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    [SecuritySafeCritical]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
}

public struct AsyncWrapperTaskMethodBuilder
{
    public static int InUseCounter { get; private set; }

    private AsyncTaskMethodBuilder _methodBuilder;

    public static AsyncWrapperTaskMethodBuilder Create()
    {
        ++InUseCounter;
        return default;
    }

    public Task Task => _methodBuilder.Task;

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        => _methodBuilder.Start(ref stateMachine);

    public void SetStateMachine(IAsyncStateMachine stateMachine) => _methodBuilder.SetStateMachine(stateMachine);

    public void SetResult()
    {
        --InUseCounter;
        _methodBuilder.SetResult();
    }

    public void SetException(Exception exception)
    {
        --InUseCounter;
        _methodBuilder.SetException(exception);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    [SecuritySafeCritical]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
}