using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

internal class InProcessNoEmitBuilder : IBuilder
{
    // It does nothing, there is no point to run it concurrently.
    public bool GetSupportsConcurrency(BuildPartition buildPartition) => false;

    public ValueTask<BuildResult> BuildAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken)
        => new(BuildResult.Success(ArtifactsPaths.Empty));
}
