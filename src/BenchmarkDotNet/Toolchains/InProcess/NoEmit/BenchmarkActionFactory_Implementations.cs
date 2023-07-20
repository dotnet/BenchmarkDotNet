using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /*
        Design goals of the whole stuff: check the comments for BenchmarkActionBase.
     */

    // DONTTOUCH: Be VERY CAREFUL when changing the code.
    // Please, ensure that the implementation is in sync with content of BenchmarkProgram.txt

    /// <summary>Helper class that creates <see cref="BenchmarkAction"/> instances. </summary>
    public static partial class BenchmarkActionFactory
    {
        internal class BenchmarkActionVoid : BenchmarkActionBase
        {
            private readonly Action callback;
            private readonly Action unrolledCallback;

            public BenchmarkActionVoid(object instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead<Action>(instance, method, OverheadStatic, OverheadInstance);
                InvokeSingle = InvokeSingleHardcoded;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeUnroll = InvokeUnrollHardcoded;
                InvokeNoUnroll = InvokeNoUnrollHardcoded;
            }

            private static void OverheadStatic() { }
            private void OverheadInstance() { }

            private ValueTask InvokeSingleHardcoded()
            {
                callback();
                return new ValueTask();
            }

            private ValueTask<ClockSpan> InvokeUnrollHardcoded(long repeatCount, IClock clock)
            {
                var startedClock = clock.Start();
                for (long i = 0; i < repeatCount; i++)
                    unrolledCallback();
                return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
            {
                var startedClock = clock.Start();
                for (long i = 0; i < repeatCount; i++)
                    callback();
                return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            }
        }

        internal class BenchmarkAction<T> : BenchmarkActionBase
        {
            private readonly Func<T> callback;
            private readonly Func<T> unrolledCallback;
            private T result;

            public BenchmarkAction(object instance, MethodInfo method, int unrollFactor)
            {
                callback = CreateWorkloadOrOverhead<Func<T>>(instance, method, OverheadStatic, OverheadInstance);
                InvokeSingle = InvokeSingleHardcoded;

                unrolledCallback = Unroll(callback, unrollFactor);
                InvokeUnroll = InvokeUnrollHardcoded;
                InvokeNoUnroll = InvokeNoUnrollHardcoded;
            }

            private static T OverheadStatic() => default;
            private T OverheadInstance() => default;

            private ValueTask InvokeSingleHardcoded()
            {
                result = callback();
                return new ValueTask();
            }

            private ValueTask<ClockSpan> InvokeUnrollHardcoded(long repeatCount, IClock clock)
            {
                var startedClock = clock.Start();
                for (long i = 0; i < repeatCount; i++)
                    result = unrolledCallback();
                return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
            {
                var startedClock = clock.Start();
                for (long i = 0; i < repeatCount; i++)
                    result = callback();
                return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            }

            public override object LastRunResult => result;
        }

        internal class BenchmarkActionTask : BenchmarkActionBase
        {
            private readonly Func<Task> callback;
            private readonly AutoResetValueTaskSource<ClockSpan> valueTaskSource = new AutoResetValueTaskSource<ClockSpan>();
            private long repeatsRemaining;
            private readonly Action continuation;
            private StartedClock startedClock;
            private TaskAwaiter currentAwaiter;

            public BenchmarkActionTask(object instance, MethodInfo method, int unrollFactor)
            {
                continuation = Continuation;
                bool isIdle = method == null;
                if (!isIdle)
                {
                    callback = CreateWorkload<Func<Task>>(instance, method);
                    InvokeSingle = InvokeSingleHardcoded;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcoded;
                }
                else
                {
                    callback = Overhead;
                    InvokeSingle = InvokeSingleHardcodedOverhead;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcodedOverhead;
                }
            }

            private Task Overhead() => default;

            private ValueTask InvokeSingleHardcodedOverhead()
            {
                callback();
                return new ValueTask();
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcodedOverhead(long repeatCount, IClock clock)
            {
                repeatsRemaining = repeatCount;
                Task value = default;
                startedClock = clock.Start();
                try
                {
                    while (--repeatsRemaining >= 0)
                    {
                        value = callback();
                    }
                }
                catch (Exception)
                {
                    Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing(value);
                    throw;
                }
                return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            }

            private ValueTask InvokeSingleHardcoded()
            {
                return AwaitHelper.ToValueTaskVoid(callback());
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
            {
                repeatsRemaining = repeatCount;
                startedClock = clock.Start();
                RunTask();
                return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
            }

            private void RunTask()
            {
                try
                {
                    while (--repeatsRemaining >= 0)
                    {
                        currentAwaiter = callback().GetAwaiter();
                        if (!currentAwaiter.IsCompleted)
                        {
                            currentAwaiter.UnsafeOnCompleted(continuation);
                            return;
                        }
                        currentAwaiter.GetResult();
                    }
                }
                catch (Exception e)
                {
                    SetException(e);
                    return;
                }
                var clockspan = startedClock.GetElapsed();
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetResult(clockspan);
            }

            private void Continuation()
            {
                try
                {
                    currentAwaiter.GetResult();
                }
                catch (Exception e)
                {
                    SetException(e);
                    return;
                }
                RunTask();
            }

            private void SetException(Exception e)
            {
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetException(e);
            }
        }

        internal class BenchmarkActionTask<T> : BenchmarkActionBase
        {
            private readonly Func<Task<T>> callback;
            private readonly AutoResetValueTaskSource<ClockSpan> valueTaskSource = new AutoResetValueTaskSource<ClockSpan>();
            private long repeatsRemaining;
            private readonly Action continuation;
            private StartedClock startedClock;
            private TaskAwaiter<T> currentAwaiter;
            private T result;

            public BenchmarkActionTask(object instance, MethodInfo method, int unrollFactor)
            {
                continuation = Continuation;
                bool isIdle = method == null;
                if (!isIdle)
                {
                    callback = CreateWorkload<Func<Task<T>>>(instance, method);
                    InvokeSingle = InvokeSingleHardcoded;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcoded;
                }
                else
                {
                    callback = Overhead;
                    InvokeSingle = InvokeSingleHardcodedOverhead;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcodedOverhead;
                }
            }

            private Task<T> Overhead() => default;

            private ValueTask InvokeSingleHardcodedOverhead()
            {
                callback();
                return new ValueTask();
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcodedOverhead(long repeatCount, IClock clock)
            {
                repeatsRemaining = repeatCount;
                Task<T> value = default;
                startedClock = clock.Start();
                try
                {
                    while (--repeatsRemaining >= 0)
                    {
                        value = callback();
                    }
                }
                catch (Exception)
                {
                    Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing(value);
                    throw;
                }
                return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            }

            private ValueTask InvokeSingleHardcoded()
            {
                return AwaitHelper.ToValueTaskVoid(callback());
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
            {
                repeatsRemaining = repeatCount;
                startedClock = clock.Start();
                RunTask();
                return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
            }

            private void RunTask()
            {
                try
                {
                    while (--repeatsRemaining >= 0)
                    {
                        currentAwaiter = callback().GetAwaiter();
                        if (!currentAwaiter.IsCompleted)
                        {
                            currentAwaiter.UnsafeOnCompleted(continuation);
                            return;
                        }
                        result = currentAwaiter.GetResult();
                    }
                }
                catch (Exception e)
                {
                    SetException(e);
                    return;
                }
                var clockspan = startedClock.GetElapsed();
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetResult(clockspan);
            }

            private void Continuation()
            {
                try
                {
                    result = currentAwaiter.GetResult();
                }
                catch (Exception e)
                {
                    SetException(e);
                    return;
                }
                RunTask();
            }

            private void SetException(Exception e)
            {
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetException(e);
            }

            public override object LastRunResult => result;
        }

        internal class BenchmarkActionValueTask : BenchmarkActionBase
        {
            private readonly Func<ValueTask> callback;
            private readonly AutoResetValueTaskSource<ClockSpan> valueTaskSource = new AutoResetValueTaskSource<ClockSpan>();
            private long repeatsRemaining;
            private readonly Action continuation;
            private StartedClock startedClock;
            private ValueTaskAwaiter currentAwaiter;

            public BenchmarkActionValueTask(object instance, MethodInfo method, int unrollFactor)
            {
                continuation = Continuation;
                bool isIdle = method == null;
                if (!isIdle)
                {
                    callback = CreateWorkload<Func<ValueTask>>(instance, method);
                    InvokeSingle = InvokeSingleHardcoded;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcoded;
                }
                else
                {
                    callback = Overhead;
                    InvokeSingle = InvokeSingleHardcodedOverhead;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcodedOverhead;
                }
            }

            private ValueTask Overhead() => default;

            private ValueTask InvokeSingleHardcodedOverhead()
            {
                callback();
                return new ValueTask();
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcodedOverhead(long repeatCount, IClock clock)
            {
                repeatsRemaining = repeatCount;
                ValueTask value = default;
                startedClock = clock.Start();
                try
                {
                    while (--repeatsRemaining >= 0)
                    {
                        value = callback();
                    }
                }
                catch (Exception)
                {
                    Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing(value);
                    throw;
                }
                return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            }

            private ValueTask InvokeSingleHardcoded()
            {
                return AwaitHelper.ToValueTaskVoid(callback());
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
            {
                repeatsRemaining = repeatCount;
                startedClock = clock.Start();
                RunTask();
                return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
            }

            private void RunTask()
            {
                try
                {
                    while (--repeatsRemaining >= 0)
                    {
                        currentAwaiter = callback().GetAwaiter();
                        if (!currentAwaiter.IsCompleted)
                        {
                            currentAwaiter.UnsafeOnCompleted(continuation);
                            return;
                        }
                        currentAwaiter.GetResult();
                    }
                }
                catch (Exception e)
                {
                    SetException(e);
                    return;
                }
                var clockspan = startedClock.GetElapsed();
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetResult(clockspan);
            }

            private void Continuation()
            {
                try
                {
                    currentAwaiter.GetResult();
                }
                catch (Exception e)
                {
                    SetException(e);
                    return;
                }
                RunTask();
            }

            private void SetException(Exception e)
            {
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetException(e);
            }
        }

        internal class BenchmarkActionValueTask<T> : BenchmarkActionBase
        {
            private readonly Func<ValueTask<T>> callback;
            private readonly AutoResetValueTaskSource<ClockSpan> valueTaskSource = new AutoResetValueTaskSource<ClockSpan>();
            private long repeatsRemaining;
            private readonly Action continuation;
            private StartedClock startedClock;
            private ValueTaskAwaiter<T> currentAwaiter;
            private T result;

            public BenchmarkActionValueTask(object instance, MethodInfo method, int unrollFactor)
            {
                continuation = Continuation;
                bool isIdle = method == null;
                if (!isIdle)
                {
                    callback = CreateWorkload<Func<ValueTask<T>>>(instance, method);
                    InvokeSingle = InvokeSingleHardcoded;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcoded;
                }
                else
                {
                    callback = Overhead;
                    InvokeSingle = InvokeSingleHardcodedOverhead;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcodedOverhead;
                }
            }

            private ValueTask<T> Overhead() => default;

            private ValueTask InvokeSingleHardcodedOverhead()
            {
                callback();
                return new ValueTask();
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcodedOverhead(long repeatCount, IClock clock)
            {
                repeatsRemaining = repeatCount;
                ValueTask<T> value = default;
                startedClock = clock.Start();
                try
                {
                    while (--repeatsRemaining >= 0)
                    {
                        value = callback();
                    }
                }
                catch (Exception)
                {
                    Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing(value);
                    throw;
                }
                return new ValueTask<ClockSpan>(startedClock.GetElapsed());
            }

            private ValueTask InvokeSingleHardcoded()
            {
                return AwaitHelper.ToValueTaskVoid(callback());
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
            {
                repeatsRemaining = repeatCount;
                startedClock = clock.Start();
                RunTask();
                return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
            }

            private void RunTask()
            {
                try
                {
                    while (--repeatsRemaining >= 0)
                    {
                        currentAwaiter = callback().GetAwaiter();
                        if (!currentAwaiter.IsCompleted)
                        {
                            currentAwaiter.UnsafeOnCompleted(continuation);
                            return;
                        }
                        result = currentAwaiter.GetResult();
                    }
                }
                catch (Exception e)
                {
                    SetException(e);
                    return;
                }
                var clockspan = startedClock.GetElapsed();
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetResult(clockspan);
            }

            private void Continuation()
            {
                try
                {
                    result = currentAwaiter.GetResult();
                }
                catch (Exception e)
                {
                    SetException(e);
                    return;
                }
                RunTask();
            }

            private void SetException(Exception e)
            {
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetException(e);
            }

            public override object LastRunResult => result;
        }
    }
}