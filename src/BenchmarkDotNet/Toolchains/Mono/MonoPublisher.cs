using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Mono
{
    public class MonoPublisher : IBuilder
    {
        public MonoPublisher(string customDotNetCliPath = null, string extraArguments = null, IReadOnlyList<EnvironmentVariable> environmentVariables = null)
        {
            CustomDotNetCliPath = customDotNetCliPath;
            ExtraArguments = extraArguments;
            EnvironmentVariables = environmentVariables;
        }

        private string CustomDotNetCliPath { get; }

        private string ExtraArguments { get; }

        private IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            => new DotNetCliCommand(
                    CustomDotNetCliPath,
                    ExtraArguments,
                    generateResult,
                    logger,
                    buildPartition,
                    EnvironmentVariables,
                    buildPartition.Timeout)
                .Publish().ToBuildResult(generateResult);
    }
}
