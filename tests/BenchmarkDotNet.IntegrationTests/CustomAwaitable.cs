using System;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.IntegrationTests;

public sealed class CustomAwaitable : ICriticalNotifyCompletion
{
    public CustomAwaitable GetAwaiter() => this;

    public void OnCompleted(Action continuation) => continuation();

    public void UnsafeOnCompleted(Action continuation) => continuation();

    public bool IsCompleted => true;

    public void GetResult() { }
}