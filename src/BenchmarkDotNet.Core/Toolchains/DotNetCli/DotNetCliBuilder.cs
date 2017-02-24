using System;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliBuilder : IBuilder
    {
        internal const string RestoreCommand = "restore --no-dependencies";

        internal const string Configuration = "Release";

        private string TargetFrameworkMoniker { get; }

        [PublicAPI]
        public DotNetCliBuilder(string targetFrameworkMoniker)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
        }

        /// <summary>
        /// generates project.lock.json that tells compiler where to take dlls and source from
        /// and builds executable and copies all required dll's
        /// </summary>
        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark, IResolver resolver)
        {
            var restoreResult = DotNetCliCommandExecutor.ExecuteCommand(
                RestoreCommand,
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);

            logger.WriteLineInfo($"// dotnet restore took {restoreResult.ExecutionTime.TotalSeconds:0.##}s");

            if (!restoreResult.IsSuccess)
            {
                return BuildResult.Failure(generateResult, new Exception(restoreResult.ProblemDescription));
            }

            var buildResult = Build(generateResult);

            logger.WriteLineInfo($"// dotnet build took {buildResult.ExecutionTime.TotalSeconds:0.##}s");

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

        private DotNetCliCommandExecutor.CommandResult Build(GenerateResult generateResult)
        {
            var withoutDependencies = DotNetCliCommandExecutor.ExecuteCommand(
                GetBuildCommand(TargetFrameworkMoniker, justTheProjectItself: true),
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);

            // at first we try to build the project without it's dependencies to save a LOT of time
            // in 99% of the cases it will work (the host process is running so it must be compiled!)
            if (withoutDependencies.IsSuccess)
                return withoutDependencies;

            // but the host process might have different runtime or was build in Debug, not Release, 
            // which requires all dependencies to be build anyway
            return DotNetCliCommandExecutor.ExecuteCommand(
                GetBuildCommand(TargetFrameworkMoniker, justTheProjectItself: false),
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);
        }

        internal static string GetBuildCommand(string frameworkMoniker, bool justTheProjectItself)
            => $"build --framework {frameworkMoniker} --configuration {Configuration}"
                + (justTheProjectItself ? " --no-dependencies" : string.Empty);
    }
}