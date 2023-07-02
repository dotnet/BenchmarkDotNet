using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Configs
{
    public interface IAwaitableAdapter<TAwaitable, TAwaiter>
        where TAwaiter : ICriticalNotifyCompletion
    {
        public TAwaiter GetAwaiter(ref TAwaitable awaitable);
        public bool GetIsCompleted(ref TAwaiter awaiter);
        public void GetResult(ref TAwaiter awaiter);
    }

    public interface IAwaitableAdapter<TAwaitable, TAwaiter, TResult>
        where TAwaiter : ICriticalNotifyCompletion
    {
        public TAwaiter GetAwaiter(ref TAwaitable awaitable);
        public bool GetIsCompleted(ref TAwaiter awaiter);
        public TResult GetResult(ref TAwaiter awaiter);
    }
}