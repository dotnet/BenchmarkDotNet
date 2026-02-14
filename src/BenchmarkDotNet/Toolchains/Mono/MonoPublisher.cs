using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Mono;

public class MonoPublisher(string tfm, string customDotNetCliPath) : DotNetCliPublisher(tfm, customDotNetCliPath)
{
    public override BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        => new DotNetCliCommand(
            CustomDotNetCliPath,
            generateResult.ArtifactsPaths.ProjectFilePath,
            TargetFrameworkMoniker,
            GetExtraArguments(),
            generateResult,
            logger,
            buildPartition,
            [],
            buildPartition.Timeout
        ).Publish().ToBuildResult(generateResult);

    private static string GetExtraArguments()
    {
        var runtimeIdentifier = CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier();
        // /p:RuntimeIdentifiers is set explicitly here because --self-contained requires it, see https://github.com/dotnet/sdk/issues/10566
        return $"--self-contained -r {runtimeIdentifier} /p:RuntimeIdentifiers={runtimeIdentifier}";
    }
}
