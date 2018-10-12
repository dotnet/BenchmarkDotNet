using System;
using System.IO;
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
        {
            var restoreResult = DotNetCliCommandExecutor.Restore(CustomDotNetCliPath, generateResult.ArtifactsPaths, logger, buildPartition);

            if (!restoreResult.IsSuccess)
                return BuildResult.Failure(generateResult, new Exception(restoreResult.ProblemDescription));

            var buildResult = DotNetCliCommandExecutor.Build(CustomDotNetCliPath, generateResult.ArtifactsPaths, logger, buildPartition);

            return buildResult.ToBuildResult(generateResult);
        }
    }
}
