using BenchmarkDotNet.Configs;
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

    public struct EmptyAwaiter : ICriticalNotifyCompletion
    {
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
            => throw new NotImplementedException();

        void INotifyCompletion.OnCompleted(Action continuation)
            => throw new NotImplementedException();
    }

    public abstract class AsyncBenchmarkRunner : IDisposable, ICriticalNotifyCompletion
    {
        // The continuation callback moves the state machine forward through the builder in the TAsyncMethodBuilderAdapter.
        private Action continuation;

        private protected void AdvanceStateMachine()
        {
            Action action = continuation;
            continuation = null;
            action();
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
            => this.continuation = continuation;

        void INotifyCompletion.OnCompleted(Action continuation)
            => this.continuation = continuation;

        public abstract ValueTask<ClockSpan> Invoke(long repeatCount, IClock clock);
        public abstract ValueTask InvokeSingle();
        // TODO: make sure Dispose is called.
        public abstract void Dispose();
    }

    public abstract class AsyncBenchmarkRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter> : AsyncBenchmarkRunner
        where TFunc : struct, IFunc<TAwaitable>
        where TAsyncMethodBuilderAdapter : IAsyncMethodBuilderAdapter, new()
        where TAwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter>
        where TAwaiter : ICriticalNotifyCompletion
    {
        private AutoResetValueTaskSource<ClockSpan> valueTaskSource;
        private readonly TAwaitableAdapter awaitableAdapter;
        private readonly TFunc func;
        private long repeatsRemaining;
        private IClock clock;
        private bool isDisposed;

        public AsyncBenchmarkRunner(TFunc func, TAwaitableAdapter awaitableAdapter)
        {
            this.func = func;
            this.awaitableAdapter = awaitableAdapter;
        }

        public override ValueTask<ClockSpan> Invoke(long repeatCount, IClock clock)
        {
            repeatsRemaining = repeatCount;
            // The clock is started inside the state machine.
            this.clock = clock;

            if (valueTaskSource == null)
            {
                valueTaskSource = new AutoResetValueTaskSource<ClockSpan>();
                // Initialize and start the state machine.
                StateMachine stateMachine = default;
                stateMachine.asyncMethodBuilderAdapter = new ();
                stateMachine.asyncMethodBuilderAdapter.CreateAsyncMethodBuilder();
                stateMachine.awaitableAdapter = awaitableAdapter;
                stateMachine.owner = this;
                stateMachine.func = func;
                stateMachine.state = -1;
                stateMachine.asyncMethodBuilderAdapter.Start(ref stateMachine);
            }
            else
            {
                AdvanceStateMachine();
            }

            return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
        }

        public override void Dispose()
        {
            // Set the isDisposed flag and advance the state machine to complete the consumer.
            isDisposed = true;
            AdvanceStateMachine();
        }

        // C# compiler creates struct state machines in Release mode, so we do the same.
        private struct StateMachine : IAsyncStateMachine
        {
            internal AsyncBenchmarkRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter> owner;
            internal TAsyncMethodBuilderAdapter asyncMethodBuilderAdapter;
            internal TAwaitableAdapter awaitableAdapter;
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
                    if (state == -1)
                    {
                        if (owner.isDisposed)
                        {
                            // The owner has been disposed, we complete the consumer.
                            asyncMethodBuilderAdapter.SetResult();
                            return;
                        }

                        // The benchmark has been started, start the clock.
                        state = 0;
                        var clock = owner.clock;
                        owner.clock = default;
                        startedClock = clock.Start();
                        goto StartLoop;
                    }

                    if (state == 1)
                    {
                        state = 0;
                        awaitableAdapter.GetResult(ref currentAwaiter);
                        currentAwaiter = default;
                    }

                StartLoop:
                    while (--owner.repeatsRemaining >= 0)
                    {
                        var awaitable = func.Invoke();
                        var awaiter = awaitableAdapter.GetAwaiter(ref awaitable);
                        if (!awaitableAdapter.GetIsCompleted(ref awaiter))
                        {
                            state = 1;
                            currentAwaiter = awaiter;
                            asyncMethodBuilderAdapter.AwaitOnCompleted(ref currentAwaiter, ref this);
                            return;
                        }
                        awaitableAdapter.GetResult(ref awaiter);
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
                state = -1;
                {
                    // We hook up the continuation to the owner so the state machine can be advanced when the next benchmark iteration starts.
                    asyncMethodBuilderAdapter.AwaitOnCompleted(ref owner, ref this);
                }
                owner.valueTaskSource.SetResult(clockspan);
            }

            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) => asyncMethodBuilderAdapter.SetStateMachine(stateMachine);
        }

        public override ValueTask InvokeSingle()
        {
            var awaitable = func.Invoke();

            if (null == default(TAwaitable) && awaitable is Task task)
            {
                return new ValueTask(task);
            }

            if (typeof(TAwaitable) == typeof(ValueTask))
            {
                return (ValueTask) (object) awaitable;
            }

            var awaiter = awaitableAdapter.GetAwaiter(ref awaitable);
            if (awaitableAdapter.GetIsCompleted(ref awaiter))
            {
                try
                {
                    awaitableAdapter.GetResult(ref awaiter);
                }
                catch (Exception e)
                {
                    return new ValueTask(Task.FromException(e));
                }
                return new ValueTask();
            }

            ToValueTaskVoidStateMachine stateMachine = default;
            stateMachine.builder = AsyncValueTaskMethodBuilder.Create();
            stateMachine.awaitableAdapter = awaitableAdapter;
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
            internal TAwaitableAdapter awaitableAdapter;
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
                    awaitableAdapter.GetResult(ref awaiter);
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

    public sealed class AsyncWorkloadRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter>
        : AsyncBenchmarkRunner<TFunc, TAsyncMethodBuilderAdapter, AsyncWorkloadRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter>.AwaitableAdapter, TAwaitable, TAwaiter>
        where TFunc : struct, IFunc<TAwaitable>
        where TAsyncMethodBuilderAdapter : IAsyncMethodBuilderAdapter, new()
        where TAwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter>, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        public AsyncWorkloadRunner(TFunc func) : base(func, new AwaitableAdapter() { userAdapter = new TAwaitableAdapter() }) { }

        public struct AwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter>
        {
            internal TAwaitableAdapter userAdapter;

            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TAwaiter GetAwaiter(ref TAwaitable awaitable)
                => userAdapter.GetAwaiter(ref awaitable);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public bool GetIsCompleted(ref TAwaiter awaiter)
                => userAdapter.GetIsCompleted(ref awaiter);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void GetResult(ref TAwaiter awaiter)
                => userAdapter.GetResult(ref awaiter);
        }
    }

    public sealed class AsyncOverheadRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitable, TAwaiter>
        : AsyncBenchmarkRunner<TFunc, TAsyncMethodBuilderAdapter, AsyncOverheadRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitable, TAwaiter>.AwaitableAdapter, TAwaitable, TAwaiter>
        where TFunc : struct, IFunc<TAwaitable>
        where TAsyncMethodBuilderAdapter : IAsyncMethodBuilderAdapter, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        public AsyncOverheadRunner(TFunc func) : base(func, new ()) { }

        public struct AwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter>
        {
            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TAwaiter GetAwaiter(ref TAwaitable awaitable)
                => default;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public bool GetIsCompleted(ref TAwaiter awaiter)
                => true;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void GetResult(ref TAwaiter awaiter) { }
        }
    }

    public sealed class AsyncWorkloadRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter, TResult>
        : AsyncBenchmarkRunner<TFunc, TAsyncMethodBuilderAdapter, AsyncWorkloadRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitableAdapter, TAwaitable, TAwaiter, TResult>.AwaitableAdapter, TAwaitable, TAwaiter>
        where TFunc : struct, IFunc<TAwaitable>
        where TAsyncMethodBuilderAdapter : IAsyncMethodBuilderAdapter, new()
        where TAwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter, TResult>, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        public AsyncWorkloadRunner(TFunc func) : base(func, new AwaitableAdapter() { userAdapter = new TAwaitableAdapter() }) { }

        public struct AwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter>
        {
            internal TAwaitableAdapter userAdapter;

            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TAwaiter GetAwaiter(ref TAwaitable awaitable)
                => userAdapter.GetAwaiter(ref awaitable);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public bool GetIsCompleted(ref TAwaiter awaiter)
                => userAdapter.GetIsCompleted(ref awaiter);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult(ref TAwaiter awaiter)
                => GetResultNoInlining(ref awaiter);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private TResult GetResultNoInlining(ref TAwaiter awaiter)
                => userAdapter.GetResult(ref awaiter);
        }
    }

    public sealed class AsyncOverheadRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitable, TAwaiter, TResult>
        : AsyncBenchmarkRunner<TFunc, TAsyncMethodBuilderAdapter, AsyncOverheadRunner<TFunc, TAsyncMethodBuilderAdapter, TAwaitable, TAwaiter, TResult>.AwaitableAdapter, TAwaitable, TAwaiter>
        where TFunc : struct, IFunc<TAwaitable>
        where TAsyncMethodBuilderAdapter : IAsyncMethodBuilderAdapter, new()
        where TAwaiter : ICriticalNotifyCompletion
    {
        public AsyncOverheadRunner(TFunc func) : base(func, new ()) { }

        public struct AwaitableAdapter : IAwaitableAdapter<TAwaitable, TAwaiter>
        {
            // Make sure the methods are called without inlining.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TAwaiter GetAwaiter(ref TAwaitable awaitable)
                => default;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public bool GetIsCompleted(ref TAwaiter awaiter)
                => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult(ref TAwaiter awaiter)
            {
                GetResultNoInlining(ref awaiter);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
#pragma warning disable IDE0060 // Remove unused parameter
            private void GetResultNoInlining(ref TAwaiter awaiter) { }
#pragma warning restore IDE0060 // Remove unused parameter
        }
    }
}