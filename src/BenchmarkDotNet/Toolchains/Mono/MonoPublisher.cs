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
        public MonoPublisher(string customDotNetCliPath)
        {
            CustomDotNetCliPath = customDotNetCliPath;
            var runtimeIdentifier = CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier();

            // /p:RuntimeIdentifiers is set explicitly here because --self-contained requires it, see https://github.com/dotnet/sdk/issues/10566
            ExtraArguments = $"--self-contained -r {runtimeIdentifier} /p:UseMonoRuntime=true /p:RuntimeIdentifiers={runtimeIdentifier}";
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
