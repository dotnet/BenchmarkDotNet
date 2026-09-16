using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains;

public interface IBuilder
{
    /// <summary>
    /// Whether this partition may be built while other partitions are being built.
    /// </summary>
    bool GetSupportsConcurrency(BuildPartition buildPartition);

    /// <summary>
    /// Generates the artifacts for the partition and builds them into something the executor can run.
    /// </summary>
    ValueTask<BuildResult> BuildAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken);
}
