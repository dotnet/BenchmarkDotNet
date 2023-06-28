using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines
{
    // TODO: handle return types from GetResult.

    public interface IAwaitableConverter<TAwaitable, TAwaiter>
        where TAwaiter : ICriticalNotifyCompletion
    {
        public TAwaiter GetAwaiter(ref TAwaitable awaitable);
        public bool GetIsCompleted(ref TAwaiter awaiter);
        public void GetResult(ref TAwaiter awaiter);
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

    public interface IAsyncConsumer<TAwaitable, TAwaiter> : IAwaitableConverter<TAwaitable, TAwaiter>, IAsyncMethodBuilder
        where TAwaiter : ICriticalNotifyCompletion
    {
    }

    // We use ConfigureAwait(false) to prevent dead-locks with InProcess toolchains (it could be ran on a thread with a SynchronizationContext).
    // Using struct rather than class forces the JIT to generate specialized code that can be inlined.
    public struct TaskConsumer : IAsyncConsumer<Task, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter>
    {
        private AsyncTaskMethodBuilder builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncTaskMethodBuilder.Create();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void SetResult()
            => builder.SetResult();

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => builder.SetStateMachine(stateMachine);

        public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter(ref Task awaitable)
            => awaitable.ConfigureAwait(false).GetAwaiter();

        public bool GetIsCompleted(ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
            => awaiter.IsCompleted;

        public void GetResult(ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
            => awaiter.GetResult();
    }

    public struct TaskConsumer<T> : IAsyncConsumer<Task<T>, ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter>
    {
        private AsyncTaskMethodBuilder builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncTaskMethodBuilder.Create();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void SetResult()
            => builder.SetResult();

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => builder.SetStateMachine(stateMachine);

        public ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter GetAwaiter(ref Task<T> awaitable)
            => awaitable.ConfigureAwait(false).GetAwaiter();

        public bool GetIsCompleted(ref ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter awaiter)
            => awaiter.IsCompleted;

        public void GetResult(ref ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter awaiter)
            => awaiter.GetResult();
    }

    public struct ValueTaskConsumer : IAsyncConsumer<ValueTask, ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter>
    {
        private AsyncValueTaskMethodBuilder builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncValueTaskMethodBuilder.Create();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void SetResult()
            => builder.SetResult();

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => builder.SetStateMachine(stateMachine);

        public ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter GetAwaiter(ref ValueTask awaitable)
            => awaitable.ConfigureAwait(false).GetAwaiter();

        public bool GetIsCompleted(ref ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter)
            => awaiter.IsCompleted;

        public void GetResult(ref ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter)
            => awaiter.GetResult();
    }

    public struct ValueTaskConsumer<T> : IAsyncConsumer<ValueTask<T>, ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter>
    {
        private AsyncValueTaskMethodBuilder builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncValueTaskMethodBuilder.Create();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            => builder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void SetResult()
            => builder.SetResult();

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => builder.SetStateMachine(stateMachine);

        public ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter GetAwaiter(ref ValueTask<T> awaitable)
            => awaitable.ConfigureAwait(false).GetAwaiter();

        public bool GetIsCompleted(ref ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter awaiter)
            => awaiter.IsCompleted;

        public void GetResult(ref ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter awaiter)
            => awaiter.GetResult();
    }
}
