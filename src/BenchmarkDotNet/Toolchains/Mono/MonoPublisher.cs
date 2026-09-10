using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Mono;

internal sealed class MonoPublisher(DotNetCliSettings settings) : DotNetCliPublisher(settings)
{
    public override async ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
    {
        var result = await new DotNetCliCommand(
            CustomDotNetCliPath,
            generateResult.ArtifactsPaths.ProjectFilePath,
            TargetFrameworkMoniker,
            GetExtraArguments(),
            generateResult,
            logger,
            buildPartition,
            [],
            buildPartition.Timeout
        )
            .PublishAsync(cancellationToken)
            .ConfigureAwait(false);
        return result.ToBuildResult(generateResult);
    }

    private static string GetExtraArguments()
    {
        var runtimeIdentifier = RuntimeInformation.GetPortableRuntimeIdentifier();
        // /p:RuntimeIdentifiers is set explicitly here because --self-contained requires it, see https://github.com/dotnet/sdk/issues/10566
        return $"--self-contained -r {runtimeIdentifier} /p:RuntimeIdentifiers={runtimeIdentifier}";
    }
}
