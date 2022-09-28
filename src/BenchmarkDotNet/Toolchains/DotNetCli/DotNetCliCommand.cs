using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public class DotNetCliCommand
    {
        [PublicAPI] public string CliPath { get; }

        [PublicAPI] public string Arguments { get; }

        [PublicAPI] public GenerateResult GenerateResult { get; }

        [PublicAPI] public ILogger Logger { get; }

        [PublicAPI] public BuildPartition BuildPartition { get; }

        [PublicAPI] public IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; }

        [PublicAPI] public TimeSpan Timeout { get; }

        [PublicAPI] public bool LogOutput { get; }

        [PublicAPI] public bool RetryFailedBuildWithNoDeps { get; }

        public DotNetCliCommand(string cliPath, string arguments, GenerateResult generateResult, ILogger logger,
            BuildPartition buildPartition, IReadOnlyList<EnvironmentVariable> environmentVariables, TimeSpan timeout, bool logOutput = false,
            bool retryFailedBuildWithNoDeps = true)
        {
            CliPath = cliPath ?? DotNetCliCommandExecutor.DefaultDotNetCliPath.Value;
            Arguments = arguments;
            GenerateResult = generateResult;
            Logger = logger;
            BuildPartition = buildPartition;
            EnvironmentVariables = environmentVariables;
            Timeout = timeout;
            LogOutput = logOutput || (buildPartition is not null && buildPartition.LogBuildOutput);
            RetryFailedBuildWithNoDeps = retryFailedBuildWithNoDeps;
        }

        public DotNetCliCommand WithArguments(string arguments)
            => new (CliPath, arguments, GenerateResult, Logger, BuildPartition, EnvironmentVariables, Timeout, logOutput: LogOutput);

        public DotNetCliCommand WithCliPath(string cliPath)
            => new (cliPath, Arguments, GenerateResult, Logger, BuildPartition, EnvironmentVariables, Timeout, logOutput: LogOutput);

        [PublicAPI]
        public BuildResult RestoreThenBuild()
        {
            DotNetCliCommandExecutor.LogEnvVars(WithArguments(null));

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
            if (!buildResult.IsSuccess && RetryFailedBuildWithNoDeps) // if we fail to do the full build, we try with --no-dependencies
                buildResult = BuildNoRestoreNoDependencies();

            return buildResult.ToBuildResult(GenerateResult);
        }

        [PublicAPI]
        public BuildResult RestoreThenBuildThenPublish()
        {
            DotNetCliCommandExecutor.LogEnvVars(WithArguments(null));

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
            if (!buildResult.IsSuccess && RetryFailedBuildWithNoDeps) // if we fail to do the full build, we try with --no-dependencies
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
                GetRestoreCommand(GenerateResult.ArtifactsPaths, BuildPartition, Arguments, "restore")));

        public DotNetCliCommandResult Build()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, Arguments, "build")));

        public DotNetCliCommandResult BuildNoRestore()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, $"{Arguments} --no-restore", "build-no-restore")));

        public DotNetCliCommandResult BuildNoRestoreNoDependencies()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, $"{Arguments} --no-restore --no-dependencies", "build-no-restore-no-deps")));

        public DotNetCliCommandResult Publish()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetPublishCommand(GenerateResult.ArtifactsPaths, BuildPartition, Arguments, "publish")));

        public DotNetCliCommandResult PublishNoBuildAndNoRestore()
            => DotNetCliCommandExecutor.Execute(WithArguments(
                GetPublishCommand(GenerateResult.ArtifactsPaths, BuildPartition, $"{Arguments} --no-build --no-restore", "publish-no-build-no-restore")));

        internal static IEnumerable<string> GetAddPackagesCommands(BuildPartition buildPartition)
            => GetNuGetAddPackageCommands(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver);

        internal static string GetRestoreCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string extraArguments = null, string binLogSuffix = null)
            => new StringBuilder()
                .AppendArgument("restore")
                .AppendArgument(string.IsNullOrEmpty(artifactsPaths.PackagesDirectoryName) ? string.Empty : $"--packages \"{artifactsPaths.PackagesDirectoryName}\"")
                .AppendArgument(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .AppendArgument(extraArguments)
                .AppendArgument(GetMandatoryMsBuildSettings(buildPartition.BuildConfiguration))
                .AppendArgument(GetMsBuildBinLogArgument(buildPartition, binLogSuffix))
                .ToString();

        internal static string GetBuildCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string extraArguments = null, string binLogSuffix = null)
            => new StringBuilder()
                .AppendArgument($"build -c {buildPartition.BuildConfiguration}") // we don't need to specify TFM, our auto-generated project contains always single one
                .AppendArgument(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .AppendArgument(extraArguments)
                .AppendArgument(GetMandatoryMsBuildSettings(buildPartition.BuildConfiguration))
                .AppendArgument(string.IsNullOrEmpty(artifactsPaths.PackagesDirectoryName) ? string.Empty : $"/p:NuGetPackageRoot=\"{artifactsPaths.PackagesDirectoryName}\"")
                .AppendArgument(GetMsBuildBinLogArgument(buildPartition, binLogSuffix))
                .ToString();

        internal static string GetPublishCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string extraArguments = null, string binLogSuffix = null)
            => new StringBuilder()
                .AppendArgument($"publish -c {buildPartition.BuildConfiguration}") // we don't need to specify TFM, our auto-generated project contains always single one
                .AppendArgument(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .AppendArgument(extraArguments)
                .AppendArgument(GetMandatoryMsBuildSettings(buildPartition.BuildConfiguration))
                .AppendArgument(string.IsNullOrEmpty(artifactsPaths.PackagesDirectoryName) ? string.Empty : $"/p:NuGetPackageRoot=\"{artifactsPaths.PackagesDirectoryName}\"")
                .AppendArgument(GetMsBuildBinLogArgument(buildPartition, binLogSuffix))
                .AppendArgument($"--output \"{artifactsPaths.BinariesDirectoryPath}\"")
                .ToString();

        private static string GetMsBuildBinLogArgument(BuildPartition buildPartition, string suffix)
        {
            if (!buildPartition.GenerateMSBuildBinLog || string.IsNullOrEmpty(suffix))
                return string.Empty;

            return $"-bl:{buildPartition.ProgramName}-{suffix}.binlog";
        }

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

            return nuGetRefs.Select(BuildAddPackageCommand);
        }

        private static string GetMandatoryMsBuildSettings(string buildConfiguration)
        {
            // we use these settings to make sure that MSBuild does the job and simply quits without spawning any long living processes
            // we want to avoid "file in use" and "zombie processes" issues
            const string NoMsBuildZombieProcesses = "/p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true";
            const string EnforceOptimizations = "/p:Optimize=true";

            if (string.Equals(buildConfiguration, RuntimeInformation.DebugConfigurationName, StringComparison.OrdinalIgnoreCase))
            {
                return NoMsBuildZombieProcesses;
            }

            return $"{NoMsBuildZombieProcesses} {EnforceOptimizations}";
        }

        private static string BuildAddPackageCommand(NuGetReference reference)
        {
            var commandBuilder = new StringBuilder();
            commandBuilder.AppendArgument("add package");
            commandBuilder.AppendArgument(reference.PackageName);
            if (!string.IsNullOrWhiteSpace(reference.PackageVersion))
            {
                commandBuilder.AppendArgument("-v");
                commandBuilder.AppendArgument(reference.PackageVersion);
            }
            if (reference.PackageSource != null)
            {
                commandBuilder.AppendArgument("-s");
                commandBuilder.AppendArgument(reference.PackageSource);
            }
            if (reference.Prerelease)
            {
                commandBuilder.AppendArgument("--prerelease");
            }
            return commandBuilder.ToString();
        }
    }
}
