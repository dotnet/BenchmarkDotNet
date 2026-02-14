using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Engines;
using Perfolizer.Horology;
using System;
using System.Reflection;
using System.Threading.Tasks;

#nullable enable

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /*
        Design goals of the whole stuff: check the comments for BenchmarkActionBase.
     */

    // DONTTOUCH: Be VERY CAREFUL when changing the code.
    // Please, ensure that the implementation is in sync with content of BenchmarkType.txt
    internal static partial class BenchmarkActionFactory
    {
        [AggressivelyOptimizeMethods]
        internal sealed class BenchmarkActionVoid : BenchmarkActionBase
        {
            private readonly Action callback;
            private readonly Action unrolledCallback;

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
        internal unsafe class BenchmarkActionVoidPointer : BenchmarkActionBase
        {
            private delegate void* PointerFunc();

            private readonly PointerFunc callback;
            private readonly PointerFunc unrolledCallback;

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
        internal unsafe class BenchmarkActionByRef<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            private delegate ref T ByRefFunc();

            private readonly ByRefFunc callback;
            private readonly ByRefFunc unrolledCallback;

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
        internal unsafe class BenchmarkActionByRefReadonly<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            private delegate ref readonly T ByRefReadonlyFunc();

            private readonly ByRefReadonlyFunc callback;
            private readonly ByRefReadonlyFunc unrolledCallback;

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
        internal class BenchmarkAction<T> : BenchmarkActionBase
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;

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
        internal class BenchmarkActionTask : BenchmarkActionBase
        {
            private readonly Func<Task> callback;
            private readonly int unrollFactor;
            private WorkloadContinuerAndValueTaskSource? workloadContinuerAndValueTaskSource;
            private IClock? clock;
            private long invokeCount;

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
                if (workloadContinuerAndValueTaskSource == null)
                {
                    workloadContinuerAndValueTaskSource = new();
                    StartWorkload();
                }
                return workloadContinuerAndValueTaskSource.Continue();
            }

            private async void StartWorkload()
            {
                await WorkloadCore();
            }

            private async Task WorkloadCore()
            {
                try
                {
                    while (true)
                    {
                        await workloadContinuerAndValueTaskSource!;
                        if (workloadContinuerAndValueTaskSource.IsCompleted)
                        {
                            return;
                        }

                        var startedClock = clock!.Start();
                        while (--invokeCount >= 0)
                        {
                            await callback();
                        }
                        workloadContinuerAndValueTaskSource.SetResult(startedClock.GetElapsed());
                    }
                }
                catch (Exception e)
                {
                    workloadContinuerAndValueTaskSource!.SetException(e);
                }
            }

            public override void Complete()
                => workloadContinuerAndValueTaskSource?.Complete();
        }

        [AggressivelyOptimizeMethods]
        internal class BenchmarkActionTask<T> : BenchmarkActionBase
        {
            private readonly Func<Task<T>> callback;
            private readonly int unrollFactor;
            private WorkloadContinuerAndValueTaskSource? workloadContinuerAndValueTaskSource;
            private IClock? clock;
            private long invokeCount;

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
                if (workloadContinuerAndValueTaskSource == null)
                {
                    workloadContinuerAndValueTaskSource = new();
                    StartWorkload();
                }
                return workloadContinuerAndValueTaskSource.Continue();
            }

            private async void StartWorkload()
            {
                await WorkloadCore();
            }

            private async Task<T> WorkloadCore()
            {
                try
                {
                    while (true)
                    {
                        await workloadContinuerAndValueTaskSource!;
                        if (workloadContinuerAndValueTaskSource.IsCompleted)
                        {
                            return default!;
                        }

                        var startedClock = clock!.Start();
                        while (--invokeCount >= 0)
                        {
                            await callback();
                        }
                        workloadContinuerAndValueTaskSource.SetResult(startedClock.GetElapsed());
                    }
                }
                catch (Exception e)
                {
                    workloadContinuerAndValueTaskSource!.SetException(e);
                    return default!;
                }
            }

            public override void Complete()
                => workloadContinuerAndValueTaskSource?.Complete();
        }

        [AggressivelyOptimizeMethods]
        internal class BenchmarkActionValueTask : BenchmarkActionBase
        {
            private readonly Func<ValueTask> callback;
            private readonly int unrollFactor;
            private WorkloadContinuerAndValueTaskSource? workloadContinuerAndValueTaskSource;
            private IClock? clock;
            private long invokeCount;

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
                if (workloadContinuerAndValueTaskSource == null)
                {
                    workloadContinuerAndValueTaskSource = new();
                    StartWorkload();
                }
                return workloadContinuerAndValueTaskSource.Continue();
            }

            private async void StartWorkload()
            {
                await WorkloadCore();
            }

            private async ValueTask WorkloadCore()
            {
                try
                {
                    while (true)
                    {
                        await workloadContinuerAndValueTaskSource!;
                        if (workloadContinuerAndValueTaskSource.IsCompleted)
                        {
                            return;
                        }

                        var startedClock = clock!.Start();
                        while (--invokeCount >= 0)
                        {
                            await callback();
                        }
                        workloadContinuerAndValueTaskSource.SetResult(startedClock.GetElapsed());
                    }
                }
                catch (Exception e)
                {
                    workloadContinuerAndValueTaskSource!.SetException(e);
                }
            }

            public override void Complete()
                => workloadContinuerAndValueTaskSource?.Complete();
        }

        [AggressivelyOptimizeMethods]
        internal class BenchmarkActionValueTask<T> : BenchmarkActionBase
        {
            private readonly Func<ValueTask<T>> callback;
            private readonly int unrollFactor;
            private WorkloadContinuerAndValueTaskSource? workloadContinuerAndValueTaskSource;
            private IClock? clock;
            private long invokeCount;

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
                if (workloadContinuerAndValueTaskSource == null)
                {
                    workloadContinuerAndValueTaskSource = new();
                    StartWorkload();
                }
                return workloadContinuerAndValueTaskSource.Continue();
            }

            private async void StartWorkload()
            {
                await WorkloadCore();
            }

            private async ValueTask<T> WorkloadCore()
            {
                try
                {
                    while (true)
                    {
                        await workloadContinuerAndValueTaskSource!;
                        if (workloadContinuerAndValueTaskSource.IsCompleted)
                        {
                            return default!;
                        }

                        var startedClock = clock!.Start();
                        while (--invokeCount >= 0)
                        {
                            await callback();
                        }
                        workloadContinuerAndValueTaskSource.SetResult(startedClock.GetElapsed());
                    }
                }
                catch (Exception e)
                {
                    workloadContinuerAndValueTaskSource!.SetException(e);
                    return default!;
                }
            }

            public override void Complete()
                => workloadContinuerAndValueTaskSource?.Complete();
        }
    }
}