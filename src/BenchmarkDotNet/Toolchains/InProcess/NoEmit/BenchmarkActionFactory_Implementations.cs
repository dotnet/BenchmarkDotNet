using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
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

        private readonly struct AwaitableFunc<TAwaitable> : IFunc<TAwaitable>
        {
            private readonly Func<TAwaitable> callback;

            internal AwaitableFunc(Func<TAwaitable> callback) => this.callback = callback;
            public TAwaitable Invoke() => callback();
        }

        internal class BenchmarkActionAwaitable<TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter> : BenchmarkActionBase
            where TAsyncMethodBuilderAdapter : IAsyncMethodBuilderAdapter, new()
            where TAwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter>, new()
            where TAwaiter : ICriticalNotifyCompletion
        {
            private readonly AsyncBenchmarkRunner asyncBenchmarkRunner;

            public BenchmarkActionAwaitable(object instance, MethodInfo method, int unrollFactor)
            {
                bool isIdle = method == null;
                if (!isIdle)
                {
                    var callback = CreateWorkload<Func<TAwaitable>>(instance, method);
                    asyncBenchmarkRunner = new AsyncWorkloadRunner<AwaitableFunc<TAwaitable>, TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter>(new (callback));
                }
                else
                {
                    asyncBenchmarkRunner = new AsyncOverheadRunner<AwaitableFunc<TAwaitable>, TAsyncMethodBuilderAdapter, TAwaitable, TAwaiter>(new (Overhead));
                }
                InvokeSingle = InvokeSingleHardcoded;
                InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcoded;
            }

            private TAwaitable Overhead() => default;

            protected virtual ValueTask InvokeSingleHardcoded()
                => asyncBenchmarkRunner.InvokeSingle();

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
                => asyncBenchmarkRunner.Invoke(repeatCount, clock);
        }

        internal class BenchmarkActionAwaitable<TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter, TResult> : BenchmarkActionBase
            where TAsyncMethodBuilderAdapter : IAsyncMethodBuilderAdapter, new()
            where TAwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter, TResult>, new()
            where TAwaiter : ICriticalNotifyCompletion
        {
            private readonly AsyncBenchmarkRunner asyncBenchmarkRunner;

            public BenchmarkActionAwaitable(object instance, MethodInfo method, int unrollFactor)
            {
                bool isIdle = method == null;
                if (!isIdle)
                {
                    var callback = CreateWorkload<Func<TAwaitable>>(instance, method);
                    asyncBenchmarkRunner = new AsyncWorkloadRunner<AwaitableFunc<TAwaitable>, TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter, TResult>(new (callback));
                }
                else
                {
                    asyncBenchmarkRunner = new AsyncOverheadRunner<AwaitableFunc<TAwaitable>, TAsyncMethodBuilderAdapter, TAwaitable, TAwaiter, TResult>(new (Overhead));
                }
                InvokeSingle = InvokeSingleHardcoded;
                InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcoded;
            }

            private TAwaitable Overhead() => default;

            protected virtual ValueTask InvokeSingleHardcoded()
                => asyncBenchmarkRunner.InvokeSingle();

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
                => asyncBenchmarkRunner.Invoke(repeatCount, clock);
        }
    }
}