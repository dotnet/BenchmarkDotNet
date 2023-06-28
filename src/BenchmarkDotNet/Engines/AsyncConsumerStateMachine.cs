using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines
{
    // Using an interface instead of delegates allows the JIT to inline the call when it's used as a generic struct.
    public interface IFunc<TResult>
    {
        TResult Invoke();
    }

    internal sealed class AsyncStateMachineAdvancer : ICriticalNotifyCompletion
    {
        // The continuation callback moves the state machine forward through the builder in the TAsyncConsumer.
        private Action continuation;

        internal void Advance()
        {
            Action action = continuation;
            continuation = null;
            action();
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
            => this.continuation = continuation;

        void INotifyCompletion.OnCompleted(Action continuation)
            => this.continuation = continuation;
    }

    public sealed class AsyncBenchmarkRunner<TWorkloadFunc, TOverheadFunc, TAsyncConsumer, TAwaitable, TAwaiter> : IDisposable
        where TWorkloadFunc : struct, IFunc<TAwaitable>
        where TOverheadFunc : struct, IFunc<TAwaitable>
        where TAsyncConsumer : IAsyncConsumer<TAwaitable, TAwaiter>, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        private readonly AutoResetValueTaskSource<ClockSpan> valueTaskSource = new ();
        private readonly TWorkloadFunc workloadFunc;
        private readonly TOverheadFunc overheadFunc;
        private long repeatsRemaining;
        private IClock clock;
        private AsyncStateMachineAdvancer workloadAsyncStateMachineAdvancer;
        private AsyncStateMachineAdvancer overheadAsyncStateMachineAdvancer;
        private bool isDisposed;

        public AsyncBenchmarkRunner(TWorkloadFunc workloadFunc, TOverheadFunc overheadFunc)
        {
            this.workloadFunc = workloadFunc;
            this.overheadFunc = overheadFunc;
        }

        private void MaybeInitializeWorkload()
        {
            if (workloadAsyncStateMachineAdvancer != null)
            {
                return;
            }

            // Initialize the state machine and consumer before the workload starts.
            workloadAsyncStateMachineAdvancer = new ();
            StateMachine<TWorkloadFunc, TAsyncConsumer> stateMachine = default;
            stateMachine.consumer = new ();
            stateMachine.consumer.CreateAsyncMethodBuilder();
            stateMachine.owner = this;
            stateMachine.stateMachineAdvancer = workloadAsyncStateMachineAdvancer;
            stateMachine.func = workloadFunc;
            stateMachine.state = -1;
            stateMachine.consumer.Start(ref stateMachine);
        }

        public ValueTask<ClockSpan> InvokeWorkload(long repeatCount, IClock clock)
        {
            MaybeInitializeWorkload();
            repeatsRemaining = repeatCount;
            // The clock is started inside the state machine.
            this.clock = clock;
            workloadAsyncStateMachineAdvancer.Advance();
            this.clock = default;
            return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
        }

        private void MaybeInitializeOverhead()
        {
            if (overheadAsyncStateMachineAdvancer != null)
            {
                return;
            }

            // Initialize the state machine and consumer before the overhead starts.
            overheadAsyncStateMachineAdvancer = new ();
            StateMachine<TOverheadFunc, OverheadConsumer> stateMachine = default;
            stateMachine.consumer = new () { asyncConsumer = new () };
            stateMachine.consumer.CreateAsyncMethodBuilder();
            stateMachine.owner = this;
            stateMachine.stateMachineAdvancer = overheadAsyncStateMachineAdvancer;
            stateMachine.func = overheadFunc;
            stateMachine.state = -1;
            stateMachine.consumer.Start(ref stateMachine);
        }

        public ValueTask<ClockSpan> InvokeOverhead(long repeatCount, IClock clock)
        {
            MaybeInitializeOverhead();
            repeatsRemaining = repeatCount;
            // The clock is started inside the state machine.
            this.clock = clock;
            overheadAsyncStateMachineAdvancer.Advance();
            this.clock = default;
            return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
        }

        // TODO: make sure Dispose is called.
        public void Dispose()
        {
            // Set the isDisposed flag and advance the state machines to complete the consumers.
            isDisposed = true;
            workloadAsyncStateMachineAdvancer?.Advance();
            overheadAsyncStateMachineAdvancer?.Advance();
        }

        // C# compiler creates struct state machines in Release mode, so we do the same.
        private struct StateMachine<TFunc, TConsumer> : IAsyncStateMachine
            where TFunc : struct, IFunc<TAwaitable>
            where TConsumer : IAsyncConsumer<TAwaitable, TAwaiter>, new()
        {
            internal AsyncBenchmarkRunner<TWorkloadFunc, TOverheadFunc, TAsyncConsumer, TAwaitable, TAwaiter> owner;
            internal AsyncStateMachineAdvancer stateMachineAdvancer;
            internal TConsumer consumer;
            internal TFunc func;
            internal int state;
            private StartedClock startedClock;
            private TAwaiter currentAwaiter;

#if NETCOREAPP3_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
            public void MoveNext()
            {
                try
                {
                    if (state < 0)
                    {
                        if (state == -1)
                        {
                            // This is called when we call asyncConsumer.Start, so we just hook up the continuation
                            // to the advancer so the state machine can be moved forward when the benchmark starts.
                            state = -2;
                            consumer.AwaitOnCompleted(ref stateMachineAdvancer, ref this);
                            return;
                        }

                        if (owner.isDisposed)
                        {
                            // The owner has been disposed, we complete the consumer.
                            consumer.SetResult();
                            return;
                        }

                        // The benchmark has been started, start the clock.
                        state = 0;
                        startedClock = owner.clock.Start();
                        goto StartLoop;
                    }

                    if (state == 1)
                    {
                        state = 0;
                        GetResult();
                    }

                StartLoop:
                    while (--owner.repeatsRemaining >= 0)
                    {
                        var awaitable = func.Invoke();
                        GetAwaiter(ref awaitable);
                        if (!GetIsCompleted())
                        {
                            state = 1;
                            consumer.AwaitOnCompleted(ref currentAwaiter, ref this);
                            return;
                        }
                        GetResult();
                    }
                }
                catch (Exception e)
                {
                    currentAwaiter = default;
                    startedClock = default;
                    owner.valueTaskSource.SetException(e);
                    return;
                }
                var clockspan = startedClock.GetElapsed();
                currentAwaiter = default;
                startedClock = default;
                state = -2;
                {
                    // We hook up the continuation to the advancer so the state machine can be moved forward when the next benchmark iteration starts.
                    consumer.AwaitOnCompleted(ref stateMachineAdvancer, ref this);
                }
                owner.valueTaskSource.SetResult(clockspan);
            }

            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private void GetAwaiter(ref TAwaitable awaitable) => currentAwaiter = consumer.GetAwaiter(ref awaitable);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private bool GetIsCompleted() => consumer.GetIsCompleted(ref currentAwaiter);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private void GetResult() => consumer.GetResult(ref currentAwaiter);

            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) => consumer.SetStateMachine(stateMachine);
        }

        private struct OverheadConsumer : IAsyncConsumer<TAwaitable, TAwaiter>
        {
            internal TAsyncConsumer asyncConsumer;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CreateAsyncMethodBuilder()
                => asyncConsumer.CreateAsyncMethodBuilder();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
                => asyncConsumer.Start(ref stateMachine);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AwaitOnCompleted<TAnyAwaiter, TStateMachine>(ref TAnyAwaiter awaiter, ref TStateMachine stateMachine)
                where TAnyAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
                => asyncConsumer.AwaitOnCompleted(ref awaiter, ref stateMachine);

            public void SetResult()
                => asyncConsumer.SetResult();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetStateMachine(IAsyncStateMachine stateMachine)
                => asyncConsumer.SetStateMachine(stateMachine);

            public TAwaiter GetAwaiter(ref TAwaitable awaitable)
                => default;

            public bool GetIsCompleted(ref TAwaiter awaiter)
                => true;

            public void GetResult(ref TAwaiter awaiter) { }
        }

        public ValueTask InvokeSingle()
        {
            var asyncConsumer = new TAsyncConsumer();
            var awaitable = workloadFunc.Invoke();

            if (null == default(TAwaitable) && awaitable is Task task)
            {
                return new ValueTask(task);
            }

            if (typeof(TAwaitable) == typeof(ValueTask))
            {
                return (ValueTask) (object) awaitable;
            }

            var awaiter = asyncConsumer.GetAwaiter(ref awaitable);
            if (asyncConsumer.GetIsCompleted(ref awaiter))
            {
                try
                {
                    asyncConsumer.GetResult(ref awaiter);
                }
                catch (Exception e)
                {
                    return new ValueTask(Task.FromException(e));
                }
                return new ValueTask();
            }

            ToValueTaskVoidStateMachine stateMachine = default;
            stateMachine.builder = AsyncValueTaskMethodBuilder.Create();
            stateMachine.consumer = asyncConsumer;
            stateMachine.awaiter = awaiter;
            stateMachine.builder.Start(ref stateMachine);
            return stateMachine.builder.Task;

            //var taskCompletionSource = new TaskCompletionSource<bool>();
            //awaiter.UnsafeOnCompleted(() =>
            //{
            //    try
            //    {
            //        asyncConsumer.GetResult(ref awaiter);
            //    }
            //    catch (Exception e)
            //    {
            //        taskCompletionSource.SetException(e);
            //        return;
            //    }
            //    taskCompletionSource.SetResult(true);
            //});
            //return new ValueTask(taskCompletionSource.Task);
        }

        private struct ToValueTaskVoidStateMachine : IAsyncStateMachine
        {
            internal AsyncValueTaskMethodBuilder builder;
            internal TAsyncConsumer consumer;
            internal TAwaiter awaiter;
            private bool isStarted;

            public void MoveNext()
            {
                if (!isStarted)
                {
                    isStarted = true;
                    builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                    return;
                }

                try
                {
                    consumer.GetResult(ref awaiter);
                    builder.SetResult();
                }
                catch (Exception e)
                {
                    builder.SetException(e);
                }
            }

            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) => builder.SetStateMachine(stateMachine);
        }
    }
}