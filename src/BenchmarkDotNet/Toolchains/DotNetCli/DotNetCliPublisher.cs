using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.DotNetCli;

public class DotNetCliPublisher(string tfm, string? customDotNetCliPath = null, string? extraArguments = null, IReadOnlyList<EnvironmentVariable>? environmentVariables = null) : IBuilder
{
    public virtual BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        => new DotNetCliCommand(
            customDotNetCliPath,
            generateResult.ArtifactsPaths.ProjectFilePath,
            tfm,
            extraArguments,
            generateResult,
            logger,
            buildPartition,
            environmentVariables,
            buildPartition.Timeout
        ).RestoreThenBuildThenPublish();
}
