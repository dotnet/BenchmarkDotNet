using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Snapshot
{
    /// <summary>
    /// Snapshot tool chain builder
    /// </summary>
    /// <seealso cref="BenchmarkDotNet.Toolchains.IBuilder" />
    internal class SnapshotBuilder : IBuilder
    {
        private SnapshotBuilder()
        {

        }

        /// <summary>
        /// Get default instance of <see cref="SnapshotBuilder"/>
        /// </summary>
        public static IBuilder Default { get; } = new SnapshotBuilder();

        /// <summary>always returns success</summary>
        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            => BuildResult.Success(generateResult);
    }
}
