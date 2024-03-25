using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public class DotNetCliPublisher : IBuilder
    {
        public DotNetCliPublisher(
            string? customDotNetCliPath = null,
            string? extraArguments = null,
            IReadOnlyList<EnvironmentVariable>? environmentVariables = null)
        {
            CustomDotNetCliPath = customDotNetCliPath;
            ExtraArguments = extraArguments;
            EnvironmentVariables = environmentVariables;
        }

        private string? CustomDotNetCliPath { get; }

        private string? ExtraArguments { get; }

        private IReadOnlyList<EnvironmentVariable>? EnvironmentVariables { get; }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var cliCommand = new DotNetCliCommand(
                generateResult.ArtifactsPaths.BuildForReferencesProjectFilePath,
                CustomDotNetCliPath,
                ExtraArguments,
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
            buildResult = cliCommand.WithCsProjPath(generateResult.ArtifactsPaths.ProjectFilePath)
                .RestoreThenBuildThenPublish();

            if (buildResult.IsBuildSuccess &&
                buildPartition.RepresentativeBenchmarkCase.Job.Environment.LargeAddressAware)
            {
                LargeAddressAware.SetLargeAddressAware(generateResult.ArtifactsPaths.ExecutablePath);
            }
            return buildResult;
        }
    }
}
