using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Snapshot
{
    /// <summary>
    /// Snapshot toolchain builder
    /// </summary>
    /// <seealso cref="BenchmarkDotNet.Toolchains.IBuilder" />
    internal class SnpashotBuilder : IBuilder
    {
        private SnpashotBuilder()
        {

        }

        /// <summary>
        /// Get default instance of <see cref="SnpashotBuilder"/>
        /// </summary>
        public static IBuilder Default { get; } = new SnpashotBuilder();

        /// <summary>always returns success</summary>
        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            => BuildResult.Success(generateResult);
    }
}
