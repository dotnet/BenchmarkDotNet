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

            // restore --packages restores all packages to a dedicated folder, our Generator always creates a new folder for this in temp (see Generator.GetPackagesDirectoryPath())
            // it's mandatory for us for two reasons:
            // 1. dotnet restore installs given package with given version number only once (every next restore reuses that package)
            //    When user rebuilds the coreclr locally the version number is preserved, so when we run dotnet restore for changed package with old version it does not get replaced
            //    so we restore to a dedicated folder to avoid this problem
            //    for more see https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingDotNetCli.md#update-coreclr-using-runtime-nuget-package
            // 2. we want to use BenchmarkDotNet for CI scenarios, CI run should left the machine in same state.
            //    this is another reason why we restore to dedicated folder and remove it at the end
            // 
            // moreover we use --no-dependencies switch to rebuild and restore only the auto-generated project, not the entire solution
            // why is that? when we run dotnet restore --packages $folder the dotnet cli creates new project.assets.json in obj folder
            // it's a sign for the tool that everything needs to be rebuild. Including the project that is currently executing!!
            // So by using this switch we use existing dlls and just build the auto-generated project
            // without this we would be trying to rebuild project which is currently in use and we would be getting file in use exceptions..
            var restoreResult = DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"restore --packages {generateResult.ArtifactsPaths.PackagesDirectoryName} --no-dependencies {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);

            logger.WriteLineInfo($"// dotnet restore took {restoreResult.ExecutionTime.TotalSeconds:0.##}s");

            if (!restoreResult.IsSuccess)
                return BuildResult.Failure(generateResult, new Exception(restoreResult.ProblemDescription));

            var buildResult = DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"build -c {buildPartition.BuildConfiguration} --no-restore --no-dependencies {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);

            logger.WriteLineInfo($"// dotnet build took {buildResult.ExecutionTime.TotalSeconds:0.##}s");

            if (!buildResult.IsSuccess)
                return BuildResult.Failure(generateResult, new Exception(buildResult.ProblemDescription));

            var publishResult = DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"publish -c {buildPartition.BuildConfiguration} --no-restore --no-dependencies {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath);

            logger.WriteLineInfo($"// dotnet publish took {publishResult.ExecutionTime.TotalSeconds:0.##}s");

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
