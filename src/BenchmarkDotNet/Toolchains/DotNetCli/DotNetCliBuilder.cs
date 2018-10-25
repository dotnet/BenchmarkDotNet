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

        [PublicAPI]
        public DotNetCliBuilder(string targetFrameworkMoniker, string customDotNetCliPath = null)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            CustomDotNetCliPath = customDotNetCliPath;
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            => new DotNetCliCommand(
                    CustomDotNetCliPath,
                    string.Empty,
                    generateResult,
                    logger,
                    buildPartition,
                    Array.Empty<EnvironmentVariable>())
                .RestoreThenBuild();
    }
}
