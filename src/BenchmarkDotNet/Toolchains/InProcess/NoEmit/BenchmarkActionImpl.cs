using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Engines;
using Perfolizer.Horology;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

#pragma warning disable CA2007 // We await the returned tasks directly the same as the generated code, we don't use ConfigureAwait on purpose.

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

/*
    Design goals of the whole stuff: check the comments for BenchmarkActionBase.
 */

// DONTTOUCH: Be VERY CAREFUL when changing the code.
// Please, ensure that the implementation is in sync with content of BenchmarkType.txt
[AggressivelyOptimizeMethods]
public sealed class BenchmarkActionVoid : BenchmarkActionBase
{
    private readonly Action callback;
    private readonly Action unrolledCallback;

    [SetsRequiredMembers]
    public BenchmarkActionVoid(object? instance, MethodInfo? method, int unrollFactor)
    {
        callback = CreateWorkloadOrOverhead(instance, method);
        unrolledCallback = Unroll(callback, unrollFactor);
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private ValueTask InvokeOnce()
    {
        callback();
        return new();
    }

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            unrolledCallback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            callback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    public override void Complete() { }
}

[AggressivelyOptimizeMethods]
public unsafe class BenchmarkActionVoidPointer : BenchmarkActionBase
{
    private delegate void* PointerFunc();

    private readonly PointerFunc callback;
    private readonly PointerFunc unrolledCallback;

    [SetsRequiredMembers]
    public BenchmarkActionVoidPointer(object? instance, MethodInfo method, int unrollFactor)
    {
        callback = CreateWorkload<PointerFunc>(instance, method);
        unrolledCallback = Unroll(callback, unrollFactor);
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private ValueTask InvokeOnce()
    {
        callback();
        return new();
    }

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            unrolledCallback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            callback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    public override void Complete() { }
}

[AggressivelyOptimizeMethods]
public class BenchmarkActionByRef<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
    where T : allows ref struct
#endif
{
    private delegate ref T ByRefFunc();

    private readonly ByRefFunc callback;
    private readonly ByRefFunc unrolledCallback;

    [SetsRequiredMembers]
    public BenchmarkActionByRef(object? instance, MethodInfo method, int unrollFactor)
    {
        callback = CreateWorkload<ByRefFunc>(instance, method);
        unrolledCallback = Unroll(callback, unrollFactor);
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private ValueTask InvokeOnce()
    {
        callback();
        return new();
    }

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            unrolledCallback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            callback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    public override void Complete() { }
}

[AggressivelyOptimizeMethods]
public class BenchmarkActionByRefReadonly<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
    where T : allows ref struct
#endif
{
    private delegate ref readonly T ByRefReadonlyFunc();

    private readonly ByRefReadonlyFunc callback;
    private readonly ByRefReadonlyFunc unrolledCallback;

    [SetsRequiredMembers]
    public BenchmarkActionByRefReadonly(object? instance, MethodInfo method, int unrollFactor)
    {
        callback = CreateWorkload<ByRefReadonlyFunc>(instance, method);
        unrolledCallback = Unroll(callback, unrollFactor);
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private ValueTask InvokeOnce()
    {
        callback();
        return new();
    }

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            unrolledCallback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            callback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    public override void Complete() { }
}

[AggressivelyOptimizeMethods]
public class BenchmarkAction<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
    where T : allows ref struct
#endif
{
    private readonly Func<T> callback;
    private readonly Func<T> unrolledCallback;

    [SetsRequiredMembers]
    public BenchmarkAction(object? instance, MethodInfo method, int unrollFactor)
    {
        callback = CreateWorkload<Func<T>>(instance, method);
        unrolledCallback = Unroll(callback, unrollFactor);
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private ValueTask InvokeOnce()
    {
        callback();
        return new();
    }

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            unrolledCallback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        var startedClock = clock.Start();
        while (--invokeCount >= 0)
        {
            callback();
        }
        return new ValueTask<ClockSpan>(startedClock.GetElapsed());
    }

    public override void Complete() { }
}

[AggressivelyOptimizeMethods]
public class BenchmarkActionTask : BenchmarkActionBase
{
    private readonly Func<Task> callback;
    private readonly int unrollFactor;
    private WorkloadValueTaskSource? workloadValueTaskSource;
    private IClock? clock;
    private long invokeCount;

    [SetsRequiredMembers]
    public BenchmarkActionTask(object? instance, MethodInfo method, int unrollFactor)
    {
        callback = CreateWorkload<Func<Task>>(instance, method);
        this.unrollFactor = unrollFactor;
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private async ValueTask InvokeOnce()
        => await callback();

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
        => WorkloadActionNoUnroll(invokeCount * unrollFactor, clock);

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        this.invokeCount = invokeCount;
        this.clock = clock;
        if (workloadValueTaskSource == null)
        {
            workloadValueTaskSource = new();
            StartWorkload();
        }
        return workloadValueTaskSource.Continue();
    }

    private async void StartWorkload()
    {
        await WorkloadCore();
    }

    private async Task WorkloadCore()
    {
        try
        {
            if (await workloadValueTaskSource!.GetIsComplete())
            {
                return;
            }
            while (true)
            {
                var startedClock = clock!.Start();
                while (--invokeCount >= 0)
                {
                    await callback();
                }
                if (await workloadValueTaskSource.SetResultAndGetIsComplete(startedClock.GetElapsed()))
                {
                    return;
                }
            }
        }
        catch (Exception e)
        {
            workloadValueTaskSource!.SetException(e);
        }
    }

    public override void Complete()
        => workloadValueTaskSource?.Complete();
}

[AggressivelyOptimizeMethods]
public class BenchmarkActionTask<T> : BenchmarkActionBase
{
    private readonly Func<Task<T>> callback;
    private readonly int unrollFactor;
    private WorkloadValueTaskSource? workloadValueTaskSource;
    private IClock? clock;
    private long invokeCount;

    [SetsRequiredMembers]
    public BenchmarkActionTask(object? instance, MethodInfo method, int unrollFactor)
    {
        callback = CreateWorkload<Func<Task<T>>>(instance, method);
        this.unrollFactor = unrollFactor;
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private async ValueTask InvokeOnce()
        => await callback();

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
        => WorkloadActionNoUnroll(invokeCount * unrollFactor, clock);

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        this.invokeCount = invokeCount;
        this.clock = clock;
        if (workloadValueTaskSource == null)
        {
            workloadValueTaskSource = new();
            StartWorkload();
        }
        return workloadValueTaskSource.Continue();
    }

    private async void StartWorkload()
    {
        await WorkloadCore();
    }

    private async Task<T> WorkloadCore()
    {
        try
        {
            if (await workloadValueTaskSource!.GetIsComplete())
            {
                return default!;
            }
            while (true)
            {
                var startedClock = clock!.Start();
                while (--invokeCount >= 0)
                {
                    await callback();
                }
                if (await workloadValueTaskSource.SetResultAndGetIsComplete(startedClock.GetElapsed()))
                {
                    return default!;
                }
            }
        }
        catch (Exception e)
        {
            workloadValueTaskSource!.SetException(e);
            return default!;
        }
    }

    public override void Complete()
        => workloadValueTaskSource?.Complete();
}

[AggressivelyOptimizeMethods]
public class BenchmarkActionValueTask : BenchmarkActionBase
{
    private readonly Func<ValueTask> callback;
    private readonly int unrollFactor;
    private WorkloadValueTaskSource? workloadValueTaskSource;
    private IClock? clock;
    private long invokeCount;

    [SetsRequiredMembers]
    public BenchmarkActionValueTask(object? instance, MethodInfo method, int unrollFactor)
    {
        callback = CreateWorkload<Func<ValueTask>>(instance, method);
        this.unrollFactor = unrollFactor;
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private async ValueTask InvokeOnce()
        => await callback();

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
        => WorkloadActionNoUnroll(invokeCount * unrollFactor, clock);

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        this.invokeCount = invokeCount;
        this.clock = clock;
        if (workloadValueTaskSource == null)
        {
            workloadValueTaskSource = new();
            StartWorkload();
        }
        return workloadValueTaskSource.Continue();
    }

    private async void StartWorkload()
    {
        await WorkloadCore();
    }

    private async ValueTask WorkloadCore()
    {
        try
        {
            if (await workloadValueTaskSource!.GetIsComplete())
            {
                return;
            }
            while (true)
            {
                var startedClock = clock!.Start();
                while (--invokeCount >= 0)
                {
                    await callback();
                }
                if (await workloadValueTaskSource.SetResultAndGetIsComplete(startedClock.GetElapsed()))
                {
                    return;
                }
            }
        }
        catch (Exception e)
        {
            workloadValueTaskSource!.SetException(e);
        }
    }

    public override void Complete()
        => workloadValueTaskSource?.Complete();
}

[AggressivelyOptimizeMethods]
public class BenchmarkActionValueTask<T> : BenchmarkActionBase
{
    private readonly Func<ValueTask<T>> callback;
    private readonly int unrollFactor;
    private WorkloadValueTaskSource? workloadValueTaskSource;
    private IClock? clock;
    private long invokeCount;

    [SetsRequiredMembers]
    public BenchmarkActionValueTask(object? instance, MethodInfo method, int unrollFactor)
    {
        callback = CreateWorkload<Func<ValueTask<T>>>(instance, method);
        this.unrollFactor = unrollFactor;
        InvokeSingle = InvokeOnce;
        InvokeUnroll = WorkloadActionUnroll;
        InvokeNoUnroll = WorkloadActionNoUnroll;
    }

    private async ValueTask InvokeOnce()
        => await callback();

    private ValueTask<ClockSpan> WorkloadActionUnroll(long invokeCount, IClock clock)
        => WorkloadActionNoUnroll(invokeCount * unrollFactor, clock);

    private ValueTask<ClockSpan> WorkloadActionNoUnroll(long invokeCount, IClock clock)
    {
        this.invokeCount = invokeCount;
        this.clock = clock;
        if (workloadValueTaskSource == null)
        {
            workloadValueTaskSource = new();
            StartWorkload();
        }
        return workloadValueTaskSource.Continue();
    }

    private async void StartWorkload()
    {
        await WorkloadCore();
    }

    private async ValueTask<T> WorkloadCore()
    {
        try
        {
            if (await workloadValueTaskSource!.GetIsComplete())
            {
                return default!;
            }
            while (true)
            {
                var startedClock = clock!.Start();
                while (--invokeCount >= 0)
                {
                    await callback();
                }
                if (await workloadValueTaskSource.SetResultAndGetIsComplete(startedClock.GetElapsed()))
                {
                    return default!;
                }
            }
        }
        catch (Exception e)
        {
            workloadValueTaskSource!.SetException(e);
            return default!;
        }
    }

    public override void Complete()
        => workloadValueTaskSource?.Complete();
}
