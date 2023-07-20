using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Configs
{
    public interface IAsyncMethodBuilderAdapter
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
}