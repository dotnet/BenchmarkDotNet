using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliBuilder : IBuilder
    {
        private string TargetFrameworkMoniker { get; }

        private string CustomDotNetCliPath { get; }
        private bool LogOutput { get; }
        private bool RetryFailedBuildWithNoDeps { get; }

        [PublicAPI]
        public DotNetCliBuilder(string targetFrameworkMoniker, string? customDotNetCliPath = null, bool logOutput = false, bool retryFailedBuildWithNoDeps = true)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            CustomDotNetCliPath = customDotNetCliPath;
            LogOutput = logOutput;
            RetryFailedBuildWithNoDeps = retryFailedBuildWithNoDeps;
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            BuildResult buildResult = new DotNetCliCommand(
                    CustomDotNetCliPath,
                    string.Empty,
                    generateResult,
                    logger,
                    buildPartition,
                    Array.Empty<EnvironmentVariable>(),
                    buildPartition.Timeout,
                    logOutput: LogOutput,
                    retryFailedBuildWithNoDeps: RetryFailedBuildWithNoDeps)
                .RestoreThenBuild();
            if (buildResult.IsBuildSuccess &&
                buildPartition.RepresentativeBenchmarkCase.Job.Environment.LargeAddressAware)
            {
                LargeAddressAware.SetLargeAddressAware(generateResult.ArtifactsPaths.ExecutablePath);
            }
            return buildResult;
        }
    }
}
