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
        public Publisher(string customDotNetCliPath = null) => CustomDotNetCliPath = customDotNetCliPath;

        private string CustomDotNetCliPath { get; }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var extraArguments = DotNetCliGenerator.GetCustomArguments(buildPartition.RepresentativeBenchmark, buildPartition.Resolver);

            var publishNoDependencies = CheckResult(generateResult, Build(generateResult, buildPartition, logger, $"--no-dependencies {extraArguments}"));

            // at first we try to build the project without it's dependencies to save a LOT of time
            // in 99% of the cases it will work (the host process is running so something can be compiled!)
            if (publishNoDependencies.IsBuildSuccess)
                return publishNoDependencies;

            return CheckResult(generateResult, Build(generateResult, buildPartition, logger, extraArguments));
        }

        private DotNetCliCommandExecutor.CommandResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, string extraArguments)
        {
            bool needsIsolatedFolderForRestore = !string.IsNullOrEmpty(generateResult.ArtifactsPaths.PackagesDirectoryName);

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
                needsIsolatedFolderForRestore 
                    ? $"restore --packages {generateResult.ArtifactsPaths.PackagesDirectoryName} {extraArguments}"
                    : $"restore {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                logger);

            if (!restoreResult.IsSuccess)
                return restoreResult;

            var buildResult = DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"build -c {buildPartition.BuildConfiguration} --no-restore {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                logger);

            if (!buildResult.IsSuccess)
                return buildResult;

            return DotNetCliCommandExecutor.ExecuteCommand(
                CustomDotNetCliPath,
                $"publish -c {buildPartition.BuildConfiguration} --no-restore {extraArguments}",
                generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath,
                logger);
        }

        private static BuildResult CheckResult(GenerateResult generateResult, DotNetCliCommandExecutor.CommandResult commandResult) 
            => commandResult.IsSuccess || File.Exists(generateResult.ArtifactsPaths.ExecutablePath) // dotnet cli could have succesfully builded the program, but returned 1 as exit code because it had some warnings
                ? BuildResult.Success(generateResult)
                : BuildResult.Failure(generateResult, new Exception(commandResult.ProblemDescription));
    }
}
