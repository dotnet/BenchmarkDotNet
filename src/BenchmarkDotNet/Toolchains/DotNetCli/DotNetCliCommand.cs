using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;
using System.Text;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public class DotNetCliCommand
    {
        [PublicAPI] public string CliPath { get; }

        [PublicAPI] public string FilePath { get; }

        [PublicAPI] public string TargetFrameworkMoniker { get; }

        [PublicAPI] public string Arguments { get; }

        [PublicAPI] public GenerateResult GenerateResult { get; }

        [PublicAPI] public ILogger Logger { get; }

        [PublicAPI] public BuildPartition BuildPartition { get; }

        [PublicAPI] public IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; }

        [PublicAPI] public TimeSpan Timeout { get; }

        [PublicAPI] public bool LogOutput { get; }

        public DotNetCliCommand(string cliPath, string filePath, string tfm, string arguments, GenerateResult generateResult, ILogger logger,
            BuildPartition buildPartition, IReadOnlyList<EnvironmentVariable> environmentVariables, TimeSpan timeout, bool logOutput = false)
        {
            CliPath = cliPath.IsBlank() ? DotNetCliCommandExecutor.DefaultDotNetCliPath.Value : cliPath;
            Arguments = arguments;
            FilePath = filePath;
            TargetFrameworkMoniker = tfm;
            GenerateResult = generateResult;
            Logger = logger;
            BuildPartition = buildPartition;
            EnvironmentVariables = environmentVariables ?? [];
            Timeout = timeout;
            LogOutput = logOutput || buildPartition.LogBuildOutput;
        }

        public DotNetCliCommand WithArguments(string arguments)
            => new(CliPath, FilePath, TargetFrameworkMoniker, arguments, GenerateResult, Logger, BuildPartition, EnvironmentVariables, Timeout, LogOutput);

        public DotNetCliCommand WithCliPath(string cliPath)
            => new(cliPath, FilePath, TargetFrameworkMoniker, Arguments, GenerateResult, Logger, BuildPartition, EnvironmentVariables, Timeout, LogOutput);

        [PublicAPI]
        public async Task<BuildResult> RestoreThenBuildAsync(CancellationToken cancellationToken = default)
        {
            DotNetCliCommandExecutor.LogEnvVars(WithArguments(""));

            // there is no way to tell dotnet restore which configuration to use (https://github.com/NuGet/Home/issues/5119)
            // so when users go with custom build configuration, we must perform full build
            // which will internally restore for the right configuration
            if (BuildPartition.IsCustomBuildConfiguration)
            {
                var result = await BuildAsync(cancellationToken).ConfigureAwait(false);
                return result.ToBuildResult(GenerateResult);
            }

            // Run `dotnet restore` and `dotnet build` command.
            if (BuildPartition.ForcedNoDependenciesForIntegrationTests)
            {
                // On our CI, Integration tests take too much time, because each benchmark run rebuilds BenchmarkDotNet itself.
                // To reduce the total duration of the CI workflows, we build all the projects without dependencies

                // Run `dotnet restore` command with `--no-dependency`.
                var restoreResult = await RestoreNoDependenciesAsync(cancellationToken).ConfigureAwait(false);
                if (!restoreResult.IsSuccess)
                    return BuildResult.Failure(GenerateResult, restoreResult.AllInformation);

                // Run `dotnet build` command with `--no-restore --no-dependency`.
                var buildResult = await BuildNoRestoreNoDependenciesAsync(cancellationToken).ConfigureAwait(false);
                return buildResult.ToBuildResult(GenerateResult);
            }
            else
            {
                // Run `dotnet restore` command.
                var restoreResult = await RestoreAsync(cancellationToken).ConfigureAwait(false);
                if (!restoreResult.IsSuccess)
                    return BuildResult.Failure(GenerateResult, restoreResult.AllInformation);

                // Run `dotnet build` command with `--no-restore` command.
                var result = await BuildNoRestoreAsync(cancellationToken).ConfigureAwait(false);
                return result.ToBuildResult(GenerateResult);
            }
        }

        [PublicAPI]
        public async Task<BuildResult> RestoreThenBuildThenPublishAsync(CancellationToken cancellationToken = default)
        {
            DotNetCliCommandExecutor.LogEnvVars(WithArguments(""));

            // there is no way to tell dotnet restore which configuration to use (https://github.com/NuGet/Home/issues/5119)
            // so when users go with custom build configuration, we must perform full publish
            // which will internally restore and build for the right configuration
            if (BuildPartition.IsCustomBuildConfiguration)
            {
                var result = await PublishAsync(cancellationToken).ConfigureAwait(false);
                return result.ToBuildResult(GenerateResult);
            }

            if (BuildPartition.ForcedNoDependenciesForIntegrationTests)
            {
                // Run `dotnet restore` command with `--no-dependencies`.
                var restoreResult = await RestoreNoDependenciesAsync(cancellationToken).ConfigureAwait(false);
                if (!restoreResult.IsSuccess)
                    return BuildResult.Failure(GenerateResult, restoreResult.AllInformation);

                // Run `dotnet build` command with `--no-restore --no-dependencies`.
                var buildResult = await BuildNoRestoreNoDependenciesAsync(cancellationToken).ConfigureAwait(false);
                if (!buildResult.IsSuccess)
                    return BuildResult.Failure(GenerateResult, buildResult.AllInformation);

                // Run `dotnet publish` command with `--no-build`.
                var publishResult = await PublishNoBuildAsync(cancellationToken).ConfigureAwait(false);
                return publishResult.ToBuildResult(GenerateResult);
            }
            else
            {
                // Run `dotnet restore` command.
                var restoreResult = await RestoreAsync(cancellationToken).ConfigureAwait(false);
                if (!restoreResult.IsSuccess)
                    return BuildResult.Failure(GenerateResult, restoreResult.AllInformation);

                // Run `dotnet build` command with `--no-restore`.
                var buildResult = await BuildNoRestoreAsync(cancellationToken).ConfigureAwait(false);
                if (!buildResult.IsSuccess)
                    return BuildResult.Failure(GenerateResult, buildResult.AllInformation);

                // Run `dotnet publish` command with `--no-build`.
                var publishResult = await PublishNoBuildAsync(cancellationToken).ConfigureAwait(false);
                return publishResult.ToBuildResult(GenerateResult);
            }
        }

        public Task<DotNetCliCommandResult> RestoreAsync(CancellationToken cancellationToken = default)
            => DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetRestoreCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, Arguments, "restore")),
                cancellationToken);

        public Task<DotNetCliCommandResult> RestoreNoDependenciesAsync(CancellationToken cancellationToken = default)
            => DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetRestoreCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, $"{Arguments} --no-dependencies", "restore-no-deps")),
                cancellationToken);

        public Task<DotNetCliCommandResult> BuildAsync(CancellationToken cancellationToken = default)
            => DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, Arguments, "build")),
                cancellationToken);

        public Task<DotNetCliCommandResult> BuildNoRestoreAsync(CancellationToken cancellationToken = default)
            => DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, $"{Arguments} --no-restore", "build-no-restore")),
                cancellationToken);

        public Task<DotNetCliCommandResult> BuildNoRestoreNoDependenciesAsync(CancellationToken cancellationToken = default)
            => DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, $"{Arguments} --no-restore --no-dependencies", "build-no-restore-no-deps")),
                cancellationToken);

        public Task<DotNetCliCommandResult> PublishAsync(CancellationToken cancellationToken = default)
            => DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetPublishCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, Arguments, "publish")),
                cancellationToken);

        public Task<DotNetCliCommandResult> PublishNoBuildAsync(CancellationToken cancellationToken = default)
            => DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetPublishCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, $"{Arguments} --no-build", "publish-no-build")),
                cancellationToken);

        internal static string GetRestoreCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string filePath, string? extraArguments = null, string? binLogSuffix = null)
            => new StringBuilder(256)
                .AppendArgument("restore")
                .AppendArgument(filePath.ToRelativePath(artifactsPaths).QuoteIfNeeded())
                // restore doesn't support -f argument.
                .AppendArgument(GetArtifactsPathArguments(buildPartition))
                .AppendArgument(artifactsPaths.PackagesDirectoryName.IsBlank() ? string.Empty : $"--packages {artifactsPaths.PackagesDirectoryName.QuoteIfNeeded()}")
                .AppendArgument(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .AppendArgument(extraArguments)
                .AppendArgument(GetMandatoryMsBuildSettings(buildPartition.BuildConfiguration))
                .AppendArgument(GetMsBuildBinLogArgument(buildPartition, filePath, binLogSuffix))
                .ToString();

        internal static string GetBuildCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string filePath, string tfm, string? extraArguments = null, string? binLogSuffix = null)
            => new StringBuilder(256)
                .AppendArgument("build")
                .AppendArgument(filePath.ToRelativePath(artifactsPaths).QuoteIfNeeded())
                .AppendArgument($"-f {tfm}")
                .AppendArgument($"-c {buildPartition.BuildConfiguration}")
                .AppendArgument(GetArtifactsPathArguments(buildPartition))
                .AppendArgument(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .AppendArgument(extraArguments)
                .AppendArgument(GetMandatoryMsBuildSettings(buildPartition.BuildConfiguration))
                .AppendArgument(artifactsPaths.PackagesDirectoryName.IsBlank() ? string.Empty : $"/p:NuGetPackageRoot={artifactsPaths.PackagesDirectoryName.QuoteIfNeeded()}")
                .AppendArgument(GetMsBuildBinLogArgument(buildPartition, filePath, binLogSuffix))
                .ToString();

        internal static string GetPublishCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string filePath, string tfm, string? extraArguments = null, string? binLogSuffix = null)
            => new StringBuilder(256)
                .AppendArgument("publish")
                .AppendArgument(filePath.ToRelativePath(artifactsPaths).QuoteIfNeeded())
                .AppendArgument($"-f {tfm}")
                .AppendArgument($"-c {buildPartition.BuildConfiguration}")
                .AppendArgument(GetArtifactsPathArguments(buildPartition))
                .AppendArgument(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .AppendArgument(extraArguments)
                .AppendArgument(GetMandatoryMsBuildSettings(buildPartition.BuildConfiguration))
                .AppendArgument(artifactsPaths.PackagesDirectoryName.IsBlank() ? string.Empty : $"/p:NuGetPackageRoot={artifactsPaths.PackagesDirectoryName.QuoteIfNeeded()}")
                .AppendArgument(GetMsBuildBinLogArgument(buildPartition, filePath, binLogSuffix))
                .ToString();

        private static string GetArtifactsPathArguments(BuildPartition buildPartition)
        {
            // Don't use `--artifacts-path` for integration tests. Because it build with `no-dependencies`.
            if (buildPartition.ForcedNoDependenciesForIntegrationTests)
                return "";

            var artifactsPath = ".artifacts";
            return $"--artifacts-path {artifactsPath}";
        }

        private static string GetMsBuildBinLogArgument(BuildPartition buildPartition, string projectPath, string? suffix)
        {
            if (!buildPartition.GenerateMSBuildBinLog || suffix.IsBlank())
                return string.Empty;

            var projectName = Path.GetFileNameWithoutExtension(projectPath);

            var fileName = $"{projectName}-{suffix}.binlog".QuoteIfNeeded();
            return $"-bl:{fileName}";
        }

        private static string GetCustomMsBuildArguments(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (!benchmarkCase.Job.HasValue(InfrastructureMode.ArgumentsCharacteristic))
                return "";

            var msBuildArguments = benchmarkCase.Job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, resolver)!.OfType<MsBuildArgument>();

            return string.Join(" ", msBuildArguments.Select(arg => arg.TextRepresentation));
        }

        private static string GetMandatoryMsBuildSettings(string buildConfiguration)
        {
            // we use these settings to make sure that MSBuild does the job and simply quits without spawning any long living processes
            // we want to avoid "file in use" and "zombie processes" issues
            const string NoMsBuildZombieProcesses = "--nodeReuse:false /p:UseSharedCompilation=false /p:Deterministic=true";
            const string EnforceOptimizations = "/p:Optimize=true";

            if (string.Equals(buildConfiguration, RuntimeInformation.DebugConfigurationName, StringComparison.OrdinalIgnoreCase))
            {
                return NoMsBuildZombieProcesses;
            }

            return $"{NoMsBuildZombieProcesses} {EnforceOptimizations}";
        }
    }

    file static class DotNetCliCommandExtensions
    {
        internal static string ToRelativePath(this string path, ArtifactsPaths artifactsPaths)
        {
            var buildArtifactsDirectoryPath = $"{artifactsPaths.BuildArtifactsDirectoryPath}{Path.DirectorySeparatorChar}";
            if (path.StartsWith(buildArtifactsDirectoryPath))
                return path.Substring(buildArtifactsDirectoryPath.Length);

            return path;
        }

        internal static string QuoteIfNeeded(this string commandArg)
        {
            ArgumentNullException.ThrowIfNull(commandArg);

            if (commandArg.Length == 0)
                return "\"\"";

            if (!commandArg.Any(char.IsWhiteSpace) && !commandArg.Contains('"'))
                return commandArg;

            var builder = new StringBuilder(commandArg.Length + 2);
            builder.Append('"');

            var backslashCount = 0;
            foreach (var c in commandArg)
            {
                switch (c)
                {
                    case '\\':
                        backslashCount++;
                        continue;
                    case '"':
                        builder.Append('\\', backslashCount * 2 + 1);
                        builder.Append('"');
                        break;
                    default:
                        builder.Append('\\', backslashCount);
                        builder.Append(c);
                        break;
                }

                backslashCount = 0;
            }

            builder.Append('\\', backslashCount * 2);
            builder.Append('"');

            return builder.ToString();
        }
    }
}
