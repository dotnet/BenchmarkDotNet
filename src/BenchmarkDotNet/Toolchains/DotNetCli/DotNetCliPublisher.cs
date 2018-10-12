using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public class DotNetCliPublisher : IBuilder
    {
        public DotNetCliPublisher(string customDotNetCliPath = null, string extraArguments = null, IReadOnlyList<EnvironmentVariable> environmentVariables = null)
        {
            CustomDotNetCliPath = customDotNetCliPath;
            ExtraArguments = extraArguments;
            EnvironmentVariables = environmentVariables;
        }

        private string CustomDotNetCliPath { get; }

        private string ExtraArguments { get; }

        private IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var cliParameters = new DotNetCliCommandExecutor.CommandParameters(CustomDotNetCliPath, ExtraArguments, generateResult.ArtifactsPaths, logger, buildPartition, EnvironmentVariables);
            
            var restoreResult = DotNetCliCommandExecutor.Restore(cliParameters);

            if (!restoreResult.IsSuccess)
                return BuildResult.Failure(generateResult, new Exception(restoreResult.ProblemDescription));

            var buildResult = DotNetCliCommandExecutor.Build(cliParameters);

            if (!buildResult.IsSuccess)
                return BuildResult.Failure(generateResult, new Exception(buildResult.ProblemDescription));

            var publishResult = DotNetCliCommandExecutor.Publish(cliParameters);

            return publishResult.ToBuildResult(generateResult);
        }
    }
}
