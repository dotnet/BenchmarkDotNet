using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System;

namespace BenchmarkDotNet.Toolchains.Snapshot
{
    /// <summary>
    ///  Implementation of <see cref="IGenerator"/> for Snapshot tool chain.
    /// </summary>
    internal class SnapshotGenerator : IGenerator
    {
        private SnapshotGenerator()
        {

        }

        /// <summary>
        /// Get default instance of <see cref="SnapshotGenerator"/>
        /// </summary>
        public static IGenerator Default { get; } = new SnapshotGenerator();

        /// <summary>returns a success</summary>
        public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
            => GenerateResult.Success(null, Array.Empty<string>());
    }
}
