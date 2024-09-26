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
        {
            var cliCommand = new DotNetCliCommand(
                generateResult.ArtifactsPaths.BuildForReferencesProjectFilePath,
                CustomDotNetCliPath,
                string.Empty,
                generateResult,
                logger,
                buildPartition,
                EnvironmentVariables,
                buildPartition.Timeout);

            // We build the original project first to obtain all dlls.
            var buildResult = cliCommand.RestoreThenBuild();

            if (!buildResult.IsBuildSuccess)
                return buildResult;

            // After the dlls are built, we gather the assembly references, then build the benchmark project.
            DotNetCliBuilder.GatherReferences(generateResult.ArtifactsPaths);
            return cliCommand
                .WithArguments(ExtraArguments)
                .WithCsProjPath(generateResult.ArtifactsPaths.ProjectFilePath)
                .Publish()
                .ToBuildResult(generateResult);
        }
    }
}
