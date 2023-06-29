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

    public struct EmptyAwaiter : ICriticalNotifyCompletion
    {
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
            => throw new NotImplementedException();

        void INotifyCompletion.OnCompleted(Action continuation)
            => throw new NotImplementedException();
    }

    public abstract class AsyncBenchmarkRunner : IDisposable
    {
        public abstract ValueTask<ClockSpan> Invoke(long repeatCount, IClock clock);
        public abstract ValueTask InvokeSingle();
        public abstract void Dispose();
    }

    public abstract class AsyncBenchmarkRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter> : AsyncBenchmarkRunner
        where TFunc : struct, IFunc<TAwaitable>
        where TAsyncConsumer : IAsyncVoidConsumer<TAwaitable, TAwaiter>, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        private readonly AutoResetValueTaskSource<ClockSpan> valueTaskSource = new ();
        private readonly TFunc func;
        private long repeatsRemaining;
        private IClock clock;
        private AsyncStateMachineAdvancer asyncStateMachineAdvancer;
        private bool isDisposed;

        public AsyncBenchmarkRunner(TFunc func)
        {
            this.func = func;
        }

        private void MaybeInitializeStateMachine()
        {
            if (asyncStateMachineAdvancer != null)
            {
                return;
            }

            // Initialize the state machine and consumer before the workload starts.
            asyncStateMachineAdvancer = new ();
            StateMachine stateMachine = default;
            stateMachine.consumer = new ();
            stateMachine.consumer.CreateAsyncMethodBuilder();
            stateMachine.owner = this;
            stateMachine.stateMachineAdvancer = asyncStateMachineAdvancer;
            stateMachine.func = func;
            stateMachine.state = -1;
            stateMachine.consumer.Start(ref stateMachine);
        }

        public override ValueTask<ClockSpan> Invoke(long repeatCount, IClock clock)
        {
            MaybeInitializeStateMachine();
            repeatsRemaining = repeatCount;
            // The clock is started inside the state machine.
            this.clock = clock;
            asyncStateMachineAdvancer.Advance();
            this.clock = default;
            return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
        }

        // TODO: make sure Dispose is called.
        public override void Dispose()
        {
            // Set the isDisposed flag and advance the state machine to complete the consumer.
            isDisposed = true;
            asyncStateMachineAdvancer?.Advance();
        }

        // C# compiler creates struct state machines in Release mode, so we do the same.
        private struct StateMachine : IAsyncStateMachine
        {
            internal AsyncBenchmarkRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter> owner;
            internal AsyncStateMachineAdvancer stateMachineAdvancer;
            internal TAsyncConsumer consumer;
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
                        consumer.GetResult(ref currentAwaiter);
                        currentAwaiter = default;
                    }

                StartLoop:
                    while (--owner.repeatsRemaining >= 0)
                    {
                        var awaitable = func.Invoke();
                        var awaiter = consumer.GetAwaiter(ref awaitable);
                        if (!consumer.GetIsCompleted(ref awaiter))
                        {
                            state = 1;
                            currentAwaiter = awaiter;
                            consumer.AwaitOnCompleted(ref currentAwaiter, ref this);
                            return;
                        }
                        consumer.GetResult(ref awaiter);
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

            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) => consumer.SetStateMachine(stateMachine);
        }

        public override ValueTask InvokeSingle()
        {
            var asyncConsumer = new TAsyncConsumer();
            var awaitable = func.Invoke();

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

            // TODO: remove the commented code after the statemachine is verified working.
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

    public sealed class AsyncWorkloadRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter> : AsyncBenchmarkRunner<TFunc, AsyncWorkloadRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter>.AsyncConsumer, TAwaitable, TAwaiter>
        where TFunc : struct, IFunc<TAwaitable>
        where TAsyncConsumer : IAsyncVoidConsumer<TAwaitable, TAwaiter>, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        public AsyncWorkloadRunner(TFunc func) : base(func) { }

        public struct AsyncConsumer : IAsyncVoidConsumer<TAwaitable, TAwaiter>
        {
            internal TAsyncConsumer asyncConsumer;

            public void CreateAsyncMethodBuilder()
            {
                asyncConsumer = new ();
                asyncConsumer.CreateAsyncMethodBuilder();
            }

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

            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TAwaiter GetAwaiter(ref TAwaitable awaitable)
                => asyncConsumer.GetAwaiter(ref awaitable);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public bool GetIsCompleted(ref TAwaiter awaiter)
                => asyncConsumer.GetIsCompleted(ref awaiter);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void GetResult(ref TAwaiter awaiter)
                => asyncConsumer.GetResult(ref awaiter);
        }
    }

    public sealed class AsyncOverheadRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter> : AsyncBenchmarkRunner<TFunc, AsyncOverheadRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter>.AsyncConsumer, EmptyAwaiter, EmptyAwaiter>
        where TFunc : struct, IFunc<EmptyAwaiter>
        where TAsyncConsumer : IAsyncVoidConsumer<TAwaitable, TAwaiter>, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        public AsyncOverheadRunner(TFunc func) : base(func) { }

        public struct AsyncConsumer : IAsyncVoidConsumer<EmptyAwaiter, EmptyAwaiter>
        {
            internal TAsyncConsumer asyncConsumer;

            public void CreateAsyncMethodBuilder()
            {
                asyncConsumer = new ();
                asyncConsumer.CreateAsyncMethodBuilder();
            }

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

            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public EmptyAwaiter GetAwaiter(ref EmptyAwaiter awaitable)
                => default;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public bool GetIsCompleted(ref EmptyAwaiter awaiter)
                => true;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void GetResult(ref EmptyAwaiter awaiter) { }
        }
    }

    public sealed class AsyncWorkloadRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter, TResult> : AsyncBenchmarkRunner<TFunc, AsyncWorkloadRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter, TResult>.AsyncConsumer, TAwaitable, TAwaiter>
        where TFunc : struct, IFunc<TAwaitable>
        where TAsyncConsumer : IAsyncResultConsumer<TAwaitable, TAwaiter, TResult>, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        public AsyncWorkloadRunner(TFunc func) : base(func) { }

        public struct AsyncConsumer : IAsyncVoidConsumer<TAwaitable, TAwaiter>
        {
            internal TAsyncConsumer asyncConsumer;

            public void CreateAsyncMethodBuilder()
            {
                asyncConsumer = new ();
                asyncConsumer.CreateAsyncMethodBuilder();
            }

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

            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TAwaiter GetAwaiter(ref TAwaitable awaitable)
                => asyncConsumer.GetAwaiter(ref awaitable);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public bool GetIsCompleted(ref TAwaiter awaiter)
                => asyncConsumer.GetIsCompleted(ref awaiter);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult(ref TAwaiter awaiter)
                => GetResultNoInlining(ref awaiter);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private TResult GetResultNoInlining(ref TAwaiter awaiter)
                => asyncConsumer.GetResult(ref awaiter);
        }
    }

    public sealed class AsyncOverheadRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter, TResult> : AsyncBenchmarkRunner<TFunc, AsyncOverheadRunner<TFunc, TAsyncConsumer, TAwaitable, TAwaiter, TResult>.AsyncConsumer, EmptyAwaiter, EmptyAwaiter>
        where TFunc : struct, IFunc<EmptyAwaiter>
        where TAsyncConsumer : IAsyncResultConsumer<TAwaitable, TAwaiter, TResult>, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        public AsyncOverheadRunner(TFunc func) : base(func) { }

        public struct AsyncConsumer : IAsyncVoidConsumer<EmptyAwaiter, EmptyAwaiter>
        {
            internal TAsyncConsumer asyncConsumer;

            public void CreateAsyncMethodBuilder()
            {
                asyncConsumer = new ();
                asyncConsumer.CreateAsyncMethodBuilder();
            }

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

            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public EmptyAwaiter GetAwaiter(ref EmptyAwaiter awaitable)
                => default;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public bool GetIsCompleted(ref EmptyAwaiter awaiter)
                => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult(ref EmptyAwaiter awaiter)
            {
                GetResultNoInlining(ref awaiter);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private void GetResultNoInlining(ref EmptyAwaiter awaiter) { }
        }
    }
}