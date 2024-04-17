using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public abstract class DotNetCliBuilderBase : IBuilder
    {
        public string? CustomDotNetCliPath { get; protected set; }
        internal bool UseArtifactsPathIfSupported { get; private protected set; } = true;
        public abstract BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger);
    }

    [PublicAPI]
    public class DotNetCliBuilder : DotNetCliBuilderBase
    {
        private bool LogOutput { get; }

        [PublicAPI]
        public DotNetCliBuilder(string targetFrameworkMoniker, string? customDotNetCliPath = null, bool logOutput = false)
        {
            CustomDotNetCliPath = customDotNetCliPath;
            LogOutput = logOutput;
        }

        internal DotNetCliBuilder(string? customDotNetCliPath, bool logOutput, bool useArtifactsPathIfSupported)
        {
            CustomDotNetCliPath = customDotNetCliPath;
            LogOutput = logOutput;
            UseArtifactsPathIfSupported = useArtifactsPathIfSupported;
        }

        public override BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            BuildResult buildResult = new DotNetCliCommand(
                    CustomDotNetCliPath,
                    string.Empty,
                    generateResult,
                    logger,
                    buildPartition,
                    Array.Empty<EnvironmentVariable>(),
                    buildPartition.Timeout,
                    logOutput: LogOutput)
                .RestoreThenBuild(UseArtifactsPathIfSupported);
            if (buildResult.IsBuildSuccess &&
                buildPartition.RepresentativeBenchmarkCase.Job.Environment.LargeAddressAware)
            {
                LargeAddressAware.SetLargeAddressAware(generateResult.ArtifactsPaths.ExecutablePath);
            }
            return buildResult;
        }
    }
}
