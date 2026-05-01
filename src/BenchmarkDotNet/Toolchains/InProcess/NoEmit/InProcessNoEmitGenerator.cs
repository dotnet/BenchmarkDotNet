using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

internal class InProcessNoEmitGenerator : IGenerator
{
    public ValueTask<GenerateResult> GenerateProjectAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken)
        => new(GenerateResult.Success(ArtifactsPaths.Empty, artifactsToCleanup: []));
}