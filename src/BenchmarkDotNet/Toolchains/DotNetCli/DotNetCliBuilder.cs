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
        public virtual string RestoreCommand => "restore --no-dependencies";

        private string TargetFrameworkMoniker { get; }

        private string CustomDotNetCliPath { get; }

        [PublicAPI]
        public DotNetCliBuilder(string targetFrameworkMoniker, string customDotNetCliPath = null)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            CustomDotNetCliPath = customDotNetCliPath;
        }

        public virtual string GetBuildCommand(string frameworkMoniker, bool justTheProjectItself, string configuration)
            => $"build --framework {frameworkMoniker} --configuration {configuration} --no-restore"
               + (justTheProjectItself ? " --no-dependencies" : string.Empty);

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var extraArguments = DotNetCliGenerator.GetCustomArguments(buildPartition.RepresentativeBenchmark, buildPartition.Resolver);

            var restoreResult = DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"{RestoreCommand} {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                logger);

            if (!restoreResult.IsSuccess)
                return BuildResult.Failure(generateResult, new Exception(restoreResult.ProblemDescription));

            var buildResult = Build(generateResult, buildPartition.BuildConfiguration, extraArguments, logger);

            if (!buildResult.IsSuccess)
            {
                // dotnet cli could have succesfully builded the program, but returned 1 as exit code because it had some warnings
                // so we need to check whether the exe exists or not, if it does then it is OK
                if (File.Exists(generateResult.ArtifactsPaths.ExecutablePath))
                {
                    return BuildResult.Success(generateResult);
                }

                return BuildResult.Failure(generateResult, new Exception(buildResult.ProblemDescription));
            }

            return BuildResult.Success(generateResult);
        }

        private DotNetCliCommandExecutor.CommandResult Build(GenerateResult generateResult, string configuration, string extraArguments, ILogger logger)
        {
            var withoutDependencies = DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"{GetBuildCommand(TargetFrameworkMoniker, true, configuration)} {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                logger);

            // at first we try to build the project without it's dependencies to save a LOT of time
            // in 99% of the cases it will work (the host process is running so it must be compiled!)
            if (withoutDependencies.IsSuccess)
                return withoutDependencies;

            // but the host process might have different runtime or was build in Debug, not Release, 
            // which requires all dependencies to be build anyway
            var withDependencies = DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"{GetBuildCommand(TargetFrameworkMoniker, false, configuration)} {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                logger);

            if (withDependencies.IsSuccess)
                return withDependencies;

            // there are some crazy edge-cases, where executing dotnet build --no-restore AFTER dotnet restore will fail
            // an example: ML.NET has default values like Configuration = Debug in Directory.Build.props file
            // when we run dotnet restore for it, it restores for Debug. When we run dotnet build -c Release --no-restore, it fails because it can't find project.assets.json file
            // The problem is that dotnet cli does not allow to set the configuration from console arguments
            // But when we run dotnet build -c Release (without --no-restore) it's going to restore the right things for us ;)
            return DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"{GetBuildCommand(TargetFrameworkMoniker, false, configuration)} {extraArguments}".Replace(" --no-restore", string.Empty),
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                logger);
        }
    }
}
