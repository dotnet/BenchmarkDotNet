using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.DotNetCli;

public class DotNetCliPublisher(
    DotNetCliSettings settings,
    string? extraArguments = null,
    IReadOnlyList<EnvironmentVariable>? environmentVariables = null,
    bool logOutput = false) : IBuilder
{
    // The toolchain resolves the target framework moniker against the runtime before constructing the publisher.
    public string TargetFrameworkMoniker { get; } = settings.TargetFrameworkMoniker;
    public FileInfo? CustomDotNetCliPath { get; } = settings.CliPath;
    public string? ExtraArguments { get; } = extraArguments;
    public IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; } = environmentVariables ?? [];
    public bool LogOutput { get; } = logOutput;

    public virtual ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
        => new(new DotNetCliCommand(
            CustomDotNetCliPath,
            generateResult.ArtifactsPaths.ProjectFilePath,
            TargetFrameworkMoniker,
            ExtraArguments,
            generateResult,
            logger,
            buildPartition,
            EnvironmentVariables,
            buildPartition.Timeout,
            logOutput: LogOutput
        )
        .RestoreThenBuildThenPublishAsync(cancellationToken));
}
