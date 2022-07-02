using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Snapshot
{
    internal class SnapshotExecutor : IExecutor
    {
        private readonly ISnapshotStore store;

        public SnapshotExecutor(ISnapshotStore store)
        {
            this.store = store;
        }

        public ExecuteResult Execute(ExecuteParameters executeParameters) =>
            store?.GetResult(executeParameters) ?? ExecuteResult.CreateFailed();
    }
}
