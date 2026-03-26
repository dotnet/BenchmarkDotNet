using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

internal class InProcessNoEmitBuilder : IBuilder
{
    public ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
        => new(BuildResult.Success(generateResult));
}