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
        internal const string RestoreCommand = "restore";

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

            logger.WriteLineInfo($"dotnet restore took {restoreResult.ExecutionTime.TotalSeconds}s");

            if (!restoreResult.IsSuccess)
            {
                return BuildResult.Failure(generateResult, new Exception(restoreResult.ProblemDescription));
            }

            var buildResult = DotNetCliCommandExecutor.ExecuteCommand(
                GetBuildCommand(TargetFrameworkMoniker),
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);

            logger.WriteLineInfo($"dotnet build took {restoreResult.ExecutionTime.TotalSeconds}s");

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

        internal static string GetBuildCommand(string frameworkMoniker)
            => $"build --framework {frameworkMoniker} --configuration {Configuration}";
    }
}