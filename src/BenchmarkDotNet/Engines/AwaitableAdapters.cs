using BenchmarkDotNet.Configs;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines
{
    // Using struct rather than class forces the JIT to generate specialized code for direct calls instead of virtual, and avoids an extra allocation.
    public struct TaskAdapter : IAwaitableAdapter<Task, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter>
    {
        // We use ConfigureAwait(false) to prevent dead-locks with InProcess toolchains (it could be ran on a thread with a SynchronizationContext).
        public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter(ref Task awaitable) => awaitable.ConfigureAwait(false).GetAwaiter();
        public bool GetIsCompleted(ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) => awaiter.IsCompleted;
        public void GetResult(ref ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) => awaiter.GetResult();
    }

    public struct TaskAdapter<TResult> : IAwaitableAdapter<Task<TResult>, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter, TResult>
    {
        // We use ConfigureAwait(false) to prevent dead-locks with InProcess toolchains (it could be ran on a thread with a SynchronizationContext).
        public ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter GetAwaiter(ref Task<TResult> awaitable) => awaitable.ConfigureAwait(false).GetAwaiter();
        public bool GetIsCompleted(ref ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) => awaiter.IsCompleted;
        public TResult GetResult(ref ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) => awaiter.GetResult();
    }

    // Using struct rather than class forces the JIT to generate specialized code that can be inlined, and avoids an extra allocation.
    public struct ValueTaskAdapter : IAwaitableAdapter<ValueTask, ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter>
    {
        // We use ConfigureAwait(false) to prevent dead-locks with InProcess toolchains (it could be ran on a thread with a SynchronizationContext).
        public ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter GetAwaiter(ref ValueTask awaitable) => awaitable.ConfigureAwait(false).GetAwaiter();
        public bool GetIsCompleted(ref ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter) => awaiter.IsCompleted;
        public void GetResult(ref ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter) => awaiter.GetResult();
    }

    public struct ValueTaskAdapter<TResult> : IAwaitableAdapter<ValueTask<TResult>, ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter, TResult>
    {
        // We use ConfigureAwait(false) to prevent dead-locks with InProcess toolchains (it could be ran on a thread with a SynchronizationContext).
        public ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter GetAwaiter(ref ValueTask<TResult> awaitable) => awaitable.ConfigureAwait(false).GetAwaiter();
        public bool GetIsCompleted(ref ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter awaiter) => awaiter.IsCompleted;
        public TResult GetResult(ref ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter awaiter) => awaiter.GetResult();
    }
}