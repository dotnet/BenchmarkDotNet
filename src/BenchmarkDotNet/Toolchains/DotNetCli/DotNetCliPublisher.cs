using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.DotNetCli;

public class DotNetCliPublisher : IBuilder
{
    public string TargetFrameworkMoniker { get; }
    public string CustomDotNetCliPath { get; }
    public string ExtraArguments { get; }
    public IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; }
    public bool LogOutput{ get; }

    public DotNetCliPublisher(
        string tfm,
        string customDotNetCliPath = "",
        string extraArguments = "",
        IReadOnlyList<EnvironmentVariable>? environmentVariables = null,
        bool logOutput = false)
    {
        TargetFrameworkMoniker = tfm;
        CustomDotNetCliPath = customDotNetCliPath.EnsureNotNull();
        ExtraArguments = extraArguments.EnsureNotNull();
        EnvironmentVariables = environmentVariables ?? [];
        LogOutput = logOutput;
    }

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
