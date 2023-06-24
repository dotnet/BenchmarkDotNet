using BenchmarkDotNet.Engines;
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

        internal static class BenchmarkActionAsyncFactory
        {
            internal static BenchmarkActionBase Create(Type consumerType, Type awaitableType, Type awaiterType, object instance, MethodInfo method, int unrollFactor)
            {
                return (BenchmarkActionBase) Activator.CreateInstance(
                    typeof(BenchmarkActionAsync<,,>).MakeGenericType(consumerType, awaitableType, awaiterType),
                    instance,
                    method,
                    unrollFactor);
            }
        }

        internal class BenchmarkActionAsync<TAsyncConsumer, TAwaitable, TAwaiter> : BenchmarkActionBase
            where TAsyncConsumer : struct, IAsyncConsumer<TAwaitable, TAwaiter>
            where TAwaiter : ICriticalNotifyCompletion
        {
            // IBenchmarkFunc implemented via struct instead of on the class so that it can be inlined.
            private readonly struct AsyncBenchmarkFunc : IBenchmarkFunc<TAwaitable>
            {
                private readonly Func<TAwaitable> callback;

                internal AsyncBenchmarkFunc(Func<TAwaitable> callback) => this.callback = callback;
                public TAwaitable InvokeWorkload() => callback();
                public TAwaitable InvokeOverhead() => callback();
            }

            private readonly Func<TAwaitable> callback;
            private readonly AsyncBenchmarkRunner<AsyncBenchmarkFunc, TAsyncConsumer, TAwaitable, TAwaiter> asyncBenchmarkRunner;

            public BenchmarkActionAsync(object instance, MethodInfo method, int unrollFactor)
            {
                bool isIdle = method == null;
                if (!isIdle)
                {
                    callback = CreateWorkload<Func<TAwaitable>>(instance, method);
                    InvokeSingle = InvokeSingleHardcoded;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcoded;
                }
                else
                {
                    callback = Overhead;
                    InvokeSingle = InvokeSingleHardcodedOverhead;
                    InvokeUnroll = InvokeNoUnroll = InvokeNoUnrollHardcodedOverhead;
                }
                asyncBenchmarkRunner = new (new AsyncBenchmarkFunc(callback));
            }

            private TAwaitable Overhead() => default;

            private ValueTask InvokeSingleHardcodedOverhead()
            {
                callback();
                return new ValueTask();
            }

            private ValueTask<ClockSpan> InvokeNoUnrollHardcodedOverhead(long repeatCount, IClock clock)
                => asyncBenchmarkRunner.InvokeOverhead(repeatCount, clock);

            protected virtual ValueTask InvokeSingleHardcoded()
                => asyncBenchmarkRunner.InvokeSingle();

            private ValueTask<ClockSpan> InvokeNoUnrollHardcoded(long repeatCount, IClock clock)
                => asyncBenchmarkRunner.InvokeWorkload(repeatCount, clock);
        }

        internal class BenchmarkActionTask : BenchmarkActionAsync<TaskConsumer, Task, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter>
        {
            public BenchmarkActionTask(object instance, MethodInfo method, int unrollFactor) : base(instance, method, unrollFactor)
            {
            }
        }

        internal class BenchmarkActionTask<T> : BenchmarkActionAsync<TaskConsumer<T>, Task<T>, ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter>
        {
            public BenchmarkActionTask(object instance, MethodInfo method, int unrollFactor) : base(instance, method, unrollFactor)
            {
            }
        }

        internal class BenchmarkActionValueTask : BenchmarkActionAsync<ValueTaskConsumer, ValueTask, ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter>
        {
            public BenchmarkActionValueTask(object instance, MethodInfo method, int unrollFactor) : base(instance, method, unrollFactor)
            {
            }
        }

        internal class BenchmarkActionValueTask<T> : BenchmarkActionAsync<ValueTaskConsumer<T>, ValueTask<T>, ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter>
        {
            public BenchmarkActionValueTask(object instance, MethodInfo method, int unrollFactor) : base(instance, method, unrollFactor)
            {
            }
        }
    }
}