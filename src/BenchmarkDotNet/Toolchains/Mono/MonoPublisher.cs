using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.Mono;

public class MonoPublisher(string tfm, string customDotNetCliPath) : DotNetCliPublisher(tfm, customDotNetCliPath)
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
        var runtimeIdentifier = CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier();
        // /p:RuntimeIdentifiers is set explicitly here because --self-contained requires it, see https://github.com/dotnet/sdk/issues/10566
        return $"--self-contained -r {runtimeIdentifier} /p:RuntimeIdentifiers={runtimeIdentifier}";
    }
}
