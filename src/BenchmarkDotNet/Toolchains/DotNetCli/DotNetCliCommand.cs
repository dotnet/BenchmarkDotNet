using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli 
{
    public class DotNetCliCommand
    {
        /// <summary>
        /// we use these settings to make sure that MSBuild does the job and simply quits without spawning any long living processes
        /// we want to avoid "file in use" and "zombie processes" issues
        /// </summary>
        private const string MandatoryMsBuildSettings = " /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1";
        
        [PublicAPI] public string CliPath { get; }
            
        [PublicAPI] public string Arguments { get; }

        [PublicAPI] public GenerateResult GenerateResult { get; }

        [PublicAPI] public ILogger Logger { get; }

        [PublicAPI] public BuildPartition BuildPartition { get; }

        [PublicAPI] public IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; }
        
        [PublicAPI] public TimeSpan Timeout { get; }

        public DotNetCliCommand(string cliPath, string arguments, GenerateResult generateResult, ILogger logger, 
            BuildPartition buildPartition, IReadOnlyList<EnvironmentVariable> environmentVariables, TimeSpan timeout)
        {
            CliPath = cliPath;
            Arguments = arguments;
            GenerateResult = generateResult;
            Logger = logger;
            BuildPartition = buildPartition;
            EnvironmentVariables = environmentVariables;
            Timeout = timeout;
        }
            
        public DotNetCliCommand WithArguments(string arguments)
            => new DotNetCliCommand(CliPath, arguments, GenerateResult, Logger, BuildPartition, EnvironmentVariables, Timeout);

        [PublicAPI]
        public BuildResult RestoreThenBuild()
        {
            var packagesResult = AddPackages();
            if (!packagesResult.IsSuccess)
                return BuildResult.Failure(GenerateResult, packagesResult.AllInformation);

            // there is no way to do tell dotnet restore which configuration to use (https://github.com/NuGet/Home/issues/5119)
            // so when users go with custom build configuration, we must perform full build
            // which will internally restore for the right configuration
            if (BuildPartition.IsCustomBuildConfiguration)
                return Build().ToBuildResult(GenerateResult);

            var restoreResult = Restore();
            if (!restoreResult.IsSuccess)
                return BuildResult.Failure(GenerateResult, restoreResult.AllInformation);

            var buildResult = BuildNoRestore();
            if (!buildResult.IsSuccess) // if we fail to do the full build, we try with --no-dependencies
                buildResult = BuildNoRestoreNoDependencies();

            return buildResult.ToBuildResult(GenerateResult);
        }

        [PublicAPI]
        public BuildResult RestoreThenBuildThenPublish()
        {
            var packagesResult = AddPackages();
            if (!packagesResult.IsSuccess)
                return BuildResult.Failure(GenerateResult, packagesResult.AllInformation);

            // there is no way to do tell dotnet restore which configuration to use (https://github.com/NuGet/Home/issues/5119)
            // so when users go with custom build configuration, we must perform full publish
            // which will internally restore and build for the right configuration
            if (BuildPartition.IsCustomBuildConfiguration)
                return Publish().ToBuildResult(GenerateResult);

            var restoreResult = Restore();
            if (!restoreResult.IsSuccess)
                return BuildResult.Failure(GenerateResult, restoreResult.AllInformation);

            var buildResult = BuildNoRestore();
            if (!buildResult.IsSuccess) // if we fail to do the full build, we try with --no-dependencies
                buildResult = BuildNoRestoreNoDependencies();

            if (!buildResult.IsSuccess)
                return BuildResult.Failure(GenerateResult, buildResult.AllInformation);

            return PublishNoBuildAndNoRestore().ToBuildResult(GenerateResult);
        }

        public DotNetCliCommandResult AddPackages()
        {
            var executionTime = new TimeSpan(0);
            var stdOutput = new StringBuilder();
            foreach (var cmd in GetAddPackagesCommands(BuildPartition))
            {
                var result = DotNetCliCommandExecutor.Execute(WithArguments(cmd));
                if (!result.IsSuccess) return result;
                executionTime += result.ExecutionTime;
                stdOutput.Append(result.StandardOutput);
            }
            return DotNetCliCommandResult.Success(executionTime, stdOutput.ToString());
        }

        public DotNetCliCommandResult Restore()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetRestoreCommand(GenerateResult.ArtifactsPaths, BuildPartition, Arguments)));

        public DotNetCliCommandResult Build()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetBuildCommand(BuildPartition, Arguments)));

        public DotNetCliCommandResult BuildNoRestore()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetBuildCommand(BuildPartition, $"{Arguments} --no-restore")));

        public DotNetCliCommandResult BuildNoRestoreNoDependencies()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetBuildCommand(BuildPartition, $"{Arguments} --no-restore --no-dependencies")));

        public DotNetCliCommandResult Publish()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetPublishCommand(BuildPartition, Arguments)));

        public DotNetCliCommandResult PublishNoBuildAndNoRestore()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetPublishCommand(BuildPartition, $"{Arguments} --no-build --no-restore")));

        internal static IEnumerable<string> GetAddPackagesCommands(BuildPartition buildPartition)
            => GetNuGetAddPackageCommands(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver);

        internal static string GetRestoreCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string extraArguments = null) 
            => new StringBuilder()
                .Append("restore ")
                .Append(string.IsNullOrEmpty(artifactsPaths.PackagesDirectoryName) ? string.Empty : $"--packages \"{artifactsPaths.PackagesDirectoryName}\" ")
                .Append(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .Append(extraArguments)
                .Append(MandatoryMsBuildSettings)
                .ToString();
        
        internal static string GetBuildCommand(BuildPartition buildPartition, string extraArguments = null) 
            => new StringBuilder()
                .Append($"build -c {buildPartition.BuildConfiguration} ") // we don't need to specify TFM, our auto-generated project contains always single one
                .Append(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .Append(extraArguments)
                .Append(MandatoryMsBuildSettings)
                .ToString();
        
        internal static string GetPublishCommand(BuildPartition buildPartition, string extraArguments = null) 
            => new StringBuilder()
                .Append($"publish -c {buildPartition.BuildConfiguration} ") // we don't need to specify TFM, our auto-generated project contains always single one
                .Append(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .Append(extraArguments)
                .Append(MandatoryMsBuildSettings)
                .ToString();

        private static string GetCustomMsBuildArguments(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (!benchmarkCase.Job.HasValue(InfrastructureMode.ArgumentsCharacteristic))
                return null;

            var msBuildArguments = benchmarkCase.Job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, resolver).OfType<MsBuildArgument>();

            return string.Join(" ", msBuildArguments.Select(arg => arg.TextRepresentation));
        }

        private static IEnumerable<string> GetNuGetAddPackageCommands(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (!benchmarkCase.Job.HasValue(InfrastructureMode.NuGetReferencesCharacteristic))
                return Enumerable.Empty<string>();

            var nuGetRefs = benchmarkCase.Job.ResolveValue(InfrastructureMode.NuGetReferencesCharacteristic, resolver);

            return nuGetRefs.Select(x => $"add package {x.PackageName}{(string.IsNullOrWhiteSpace(x.PackageVersion) ? string.Empty : " -v " + x.PackageVersion)}");
        }
    }
}