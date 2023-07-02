using BenchmarkDotNet.Configs;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines
{
    // Using struct rather than class forces the JIT to generate specialized code for direct calls instead of virtual, and avoids an extra allocation.
    public struct AsyncTaskMethodBuilderAdapter : IAsyncMethodBuilderAdapter
    {
        // We use a type that users cannot access to prevent the async method builder from being pre-jitted with the user's type, in case the benchmark is ran with ColdStart.
        private struct EmptyStruct { }

        private AsyncTaskMethodBuilder<EmptyStruct> builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncTaskMethodBuilder<EmptyStruct>.Create();

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
    }

    public struct AsyncValueTaskMethodBuilderAdapter : IAsyncMethodBuilderAdapter
    {
        // We use a type that users cannot access to prevent the async method builder from being pre-jitted with the user's type, in case the benchmark is ran with ColdStart.
        private struct EmptyStruct { }

        private AsyncValueTaskMethodBuilder<EmptyStruct> builder;

        public void CreateAsyncMethodBuilder()
            => builder = AsyncValueTaskMethodBuilder<EmptyStruct>.Create();

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
}