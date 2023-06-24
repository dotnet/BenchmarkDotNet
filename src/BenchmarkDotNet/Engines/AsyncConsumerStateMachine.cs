using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines
{
    // Using an interface instead of delegates allows the JIT to inline the call when it's used as a generic struct.
    public interface IBenchmarkFunc<TResult>
    {
        TResult InvokeWorkload();
        TResult InvokeOverhead();
    }

    public class AsyncBenchmarkRunner<TBenchmarkFunc, TAsyncConsumer, TAwaitable, TAwaiter> : ICriticalNotifyCompletion, IDisposable
        where TBenchmarkFunc : struct, IBenchmarkFunc<TAwaitable>
        // Struct constraint allows us to create the default value and forces the JIT to generate specialized code that can be inlined.
        where TAsyncConsumer : struct, IAsyncConsumer<TAwaitable, TAwaiter>
        where TAwaiter : ICriticalNotifyCompletion
    {
        // Using struct rather than class forces the JIT to generate specialized code that can be inlined.
        // Also C# compiler uses struct state machines in Release mode, so we want to do the same.
        private struct StateMachine : IAsyncStateMachine
        {
            internal AsyncBenchmarkRunner<TBenchmarkFunc, TAsyncConsumer, TAwaitable, TAwaiter> owner;
            internal TAsyncConsumer asyncConsumer;

            public void MoveNext() => owner.MoveNext(ref this);

            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) => asyncConsumer.SetStateMachine(stateMachine);
        }

        private readonly AutoResetValueTaskSource<ClockSpan> valueTaskSource = new ();
        private TBenchmarkFunc benchmarkFunc;
        private int state = -1;
        private long repeatsRemaining;
        private StartedClock startedClock;
        private TAwaiter currentAwaiter;
        private Action continuation;

        public AsyncBenchmarkRunner(TBenchmarkFunc benchmarkFunc)
        {
            this.benchmarkFunc = benchmarkFunc;
            // Initialize the state machine and consumer before the actual workload starts.
            StateMachine stateMachine = default;
            stateMachine.asyncConsumer = new TAsyncConsumer();
            stateMachine.asyncConsumer.CreateAsyncMethodBuilder();
            stateMachine.owner = this;
            stateMachine.asyncConsumer.Start(ref stateMachine);
        }

        public ValueTask<ClockSpan> InvokeWorkload(long repeatCount, IClock clock)
        {
            repeatsRemaining = repeatCount;
            Action action = continuation;
            continuation = null;
            startedClock = clock.Start();
            // The continuation callback moves the state machine forward through the builder in the TAsyncConsumer.
            action();
            return new ValueTask<ClockSpan>(valueTaskSource, valueTaskSource.Version);
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
            => this.continuation = continuation;

        void INotifyCompletion.OnCompleted(Action continuation)
            => this.continuation = continuation;

        private void MoveNext(ref StateMachine stateMachine)
        {
            try
            {
                if (state < 0)
                {
                    if (state == -1)
                    {
                        // This is called when we call asyncConsumer.Start, so we just hook up the continuation
                        // to the owner so the state machine can be moved forward when the benchmark starts.
                        state = 0;
                        var _this = this;
                        stateMachine.asyncConsumer.AwaitOnCompleted(ref _this, ref stateMachine);
                        return;
                    }
                    // This has been disposed, we complete the consumer.
                    stateMachine.asyncConsumer.SetResult();
                    return;
                }

                if (state == 1)
                {
                    state = 0;
                    stateMachine.asyncConsumer.GetResult(ref currentAwaiter);
                }

                while (--repeatsRemaining >= 0)
                {
                    var awaitable = benchmarkFunc.InvokeWorkload();
                    currentAwaiter = stateMachine.asyncConsumer.GetAwaiter(ref awaitable);
                    if (!stateMachine.asyncConsumer.GetIsCompleted(ref currentAwaiter))
                    {
                        state = 1;
                        stateMachine.asyncConsumer.AwaitOnCompleted(ref currentAwaiter, ref stateMachine);
                        return;
                    }
                    stateMachine.asyncConsumer.GetResult(ref currentAwaiter);
                }
            }
            catch (Exception e)
            {
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetException(e);
                return;
            }
            var clockspan = startedClock.GetElapsed();
            currentAwaiter = default;
            startedClock = default;
            {
                // We hook up the continuation to the owner so the state machine can be moved forward when the next benchmark iteration starts.
                stateMachine.asyncConsumer.AwaitOnCompleted(ref stateMachine.owner, ref stateMachine);
            }
            valueTaskSource.SetResult(clockspan);
        }

        public void Dispose()
        {
            benchmarkFunc = default;
            Action action = continuation;
            continuation = null;
            // Set the state and invoke the callback for the state machine to advance to complete the consumer.
            state = -2;
            action();
        }

        public ValueTask<ClockSpan> InvokeOverhead(long repeatCount, IClock clock)
        {
            repeatsRemaining = repeatCount;
            TAwaitable value = default;
            startedClock = clock.Start();
            try
            {
                while (--repeatsRemaining >= 0)
                {
                    value = benchmarkFunc.InvokeOverhead();
                }
            }
            catch (Exception e)
            {
                currentAwaiter = default;
                startedClock = default;
                valueTaskSource.SetException(e);
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(value);
                throw;
            }
            var clockspan = startedClock.GetElapsed();
            currentAwaiter = default;
            startedClock = default;
            return new ValueTask<ClockSpan>(clockspan);
        }

        public ValueTask InvokeSingle()
        {
            var asyncConsumer = new TAsyncConsumer();
            var awaitable = benchmarkFunc.InvokeWorkload();
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
            var taskCompletionSource = new TaskCompletionSource<bool>();
            awaiter.UnsafeOnCompleted(() =>
            {
                try
                {
                    asyncConsumer.GetResult(ref awaiter);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                    return;
                }
                taskCompletionSource.SetResult(true);
            });
            return new ValueTask(taskCompletionSource.Task);
        }
    }
}