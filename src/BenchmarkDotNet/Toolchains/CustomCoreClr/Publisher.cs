using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;
using System;
using System.IO;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    public class Publisher : IBuilder
    {
        public Publisher(string targetFrameworkMoniker, string customDotNetCliPath = null, string[] filesToCopy = null) 
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            CustomDotNetCliPath = customDotNetCliPath;
            FilesToCopy = filesToCopy;
        }

        private string TargetFrameworkMoniker { get; }
        private string CustomDotNetCliPath { get; }
        private string[] FilesToCopy { get; }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var extraArguments = DotNetCliGenerator.GetCustomArguments(buildPartition.RepresentativeBenchmark, buildPartition.Resolver);

            var configurationName = buildPartition.BuildConfiguration;

            var publishResult = DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"publish -c {configurationName} {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);

            if (!publishResult.IsSuccess &&
                !File.Exists(generateResult.ArtifactsPaths.ExecutablePath)) // dotnet cli could have succesfully builded the program, but returned 1 as exit code because it had some warnings
            {
                return BuildResult.Failure(generateResult, new Exception(publishResult.ProblemDescription));
            }

            if (FilesToCopy != null)
            {
                var destinationFolder = Path.GetDirectoryName(generateResult.ArtifactsPaths.ExecutablePath);
                foreach (var fileToCopy in FilesToCopy)
                {
                    File.Copy(fileToCopy, Path.Combine(destinationFolder, Path.GetFileName(fileToCopy)), overwrite: true);
                }
            }

            return BuildResult.Success(generateResult);
        }
    }
}
