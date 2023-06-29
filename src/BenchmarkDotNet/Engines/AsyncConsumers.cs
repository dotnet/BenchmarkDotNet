using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines
{
    public interface IAwaitableConverter<TAwaitable, TAwaiter>
        where TAwaiter : ICriticalNotifyCompletion
    {
        public TAwaiter GetAwaiter(ref TAwaitable awaitable);
        public bool GetIsCompleted(ref TAwaiter awaiter);
    }

    public interface IAwaitableVoidConverter<TAwaitable, TAwaiter> : IAwaitableConverter<TAwaitable, TAwaiter>
        where TAwaiter : ICriticalNotifyCompletion
    {
        public void GetResult(ref TAwaiter awaiter);
    }

    public interface IAwaitableResultConverter<TAwaitable, TAwaiter, TResult> : IAwaitableConverter<TAwaitable, TAwaiter>
        where TAwaiter : ICriticalNotifyCompletion
    {
        public TResult GetResult(ref TAwaiter awaiter);
    }

    public interface IAsyncMethodBuilder
    {
        public void CreateAsyncMethodBuilder();
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine;
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine;
        public void SetStateMachine(IAsyncStateMachine stateMachine);
        public void SetResult();
    }

    public interface IAsyncVoidConsumer<TAwaitable, TAwaiter> : IAwaitableVoidConverter<TAwaitable, TAwaiter>, IAsyncMethodBuilder
        where TAwaiter : ICriticalNotifyCompletion
    {
    }

    public interface IAsyncResultConsumer<TAwaitable, TAwaiter, TResult> : IAwaitableResultConverter<TAwaitable, TAwaiter, TResult>, IAsyncMethodBuilder
        where TAwaiter : ICriticalNotifyCompletion
    {
    }

    // We use a type that users cannot access to prevent the async method builder from being jitted with the user's type, in case the benchmark is ran with ColdStart.
    internal struct UnusedStruct { }

    // We use ConfigureAwait(false) to prevent dead-locks with InProcess toolchains (it could be ran on a thread with a SynchronizationContext).
    // Using struct rather than class forces the JIT to generate specialized code that can be inlined, and avoids an extra allocation.
    public struct TaskConsumer : IAsyncVoidConsumer<Task, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter>
    {
        private AsyncTaskMethodBuilder<UnusedStruct> builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncTaskMethodBuilder<UnusedStruct>.Create();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void SetResult()
            => builder.SetResult(default);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => builder.SetStateMachine(stateMachine);

        public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter(ref Task awaitable)
            => awaitable.ConfigureAwait(false).GetAwaiter();

        public bool GetIsCompleted(ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
            => awaiter.IsCompleted;

        public void GetResult(ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
            => awaiter.GetResult();
    }

    public struct TaskConsumer<T> : IAsyncResultConsumer<Task<T>, ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter, T>
    {
        private AsyncTaskMethodBuilder<UnusedStruct> builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncTaskMethodBuilder<UnusedStruct>.Create();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void SetResult()
            => builder.SetResult(default);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => builder.SetStateMachine(stateMachine);

        public ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter GetAwaiter(ref Task<T> awaitable)
            => awaitable.ConfigureAwait(false).GetAwaiter();

        public bool GetIsCompleted(ref ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter awaiter)
            => awaiter.IsCompleted;

        public T GetResult(ref ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter awaiter)
            => awaiter.GetResult();
    }

    public struct ValueTaskConsumer : IAsyncVoidConsumer<ValueTask, ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter>
    {
        private AsyncValueTaskMethodBuilder<UnusedStruct> builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncValueTaskMethodBuilder<UnusedStruct>.Create();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void SetResult()
            => builder.SetResult(default);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => builder.SetStateMachine(stateMachine);

        public ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter GetAwaiter(ref ValueTask awaitable)
            => awaitable.ConfigureAwait(false).GetAwaiter();

        public bool GetIsCompleted(ref ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter)
            => awaiter.IsCompleted;

        public void GetResult(ref ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter)
            => awaiter.GetResult();
    }

    public struct ValueTaskConsumer<T> : IAsyncResultConsumer<ValueTask<T>, ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter, T>
    {
        private AsyncValueTaskMethodBuilder<UnusedStruct> builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncValueTaskMethodBuilder<UnusedStruct>.Create();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void SetResult()
            => builder.SetResult(default);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => builder.SetStateMachine(stateMachine);

        public ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter GetAwaiter(ref ValueTask<T> awaitable)
            => awaitable.ConfigureAwait(false).GetAwaiter();

        public bool GetIsCompleted(ref ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter awaiter)
            => awaiter.IsCompleted;

        public T GetResult(ref ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter awaiter)
            => awaiter.GetResult();
    }
}
