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
        [PublicAPI] public FileInfo? CliPath { get; }

        [PublicAPI] public string FilePath { get; }

        [PublicAPI] public string TargetFrameworkMoniker { get; }

        [PublicAPI] public string? Arguments { get; }

        [PublicAPI] public GenerateResult GenerateResult { get; }

        [PublicAPI] public ILogger Logger { get; }

        [PublicAPI] public BuildPartition BuildPartition { get; }

        [PublicAPI] public IReadOnlyList<EnvironmentVariable> EnvironmentVariables { get; }

        [PublicAPI] public TimeSpan Timeout { get; }

        [PublicAPI] public bool LogOutput { get; }

        public DotNetCliCommand(FileInfo? cliPath, string filePath, string tfm, string? arguments, GenerateResult generateResult, ILogger logger,
            BuildPartition buildPartition, IReadOnlyList<EnvironmentVariable> environmentVariables, TimeSpan timeout, bool logOutput = false)
        {
            CliPath = cliPath; // null means "use the default dotnet cli"; resolved in DotNetCliCommandExecutor.BuildStartInfo
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

        public DotNetCliCommand WithCliPath(FileInfo? cliPath)
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
                return await BuildAsync(cancellationToken).ConfigureAwait(false);
            }

            if (BuildPartition.ForcedNoDependenciesForIntegrationTests)
            {
                // On our CI, Integration tests take too much time, because each benchmark run rebuilds BenchmarkDotNet itself.
                // To reduce the total duration of the CI workflows, we build all the projects without dependencies
                var restoreNoDependenciesResult = await DotNetCliCommandExecutor.ExecuteAsync(
                    WithArguments(GetRestoreCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, $"{Arguments} --no-dependencies", "restore-no-deps")),
                    cancellationToken).ConfigureAwait(false);
                if (!restoreNoDependenciesResult.IsSuccess)
                    return BuildResult.Failure(GenerateResult, restoreNoDependenciesResult.AllInformation);

                var buildNoDependenciesResult = await DotNetCliCommandExecutor.ExecuteAsync(
                    WithArguments(GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, $"{Arguments} --no-restore --no-dependencies", "build-no-restore-no-deps")),
                    cancellationToken).ConfigureAwait(false);
                return buildNoDependenciesResult.ToBuildResult(GenerateResult);
            }

            var restoreResult = await DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetRestoreCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, Arguments, "restore")),
                cancellationToken).ConfigureAwait(false);
            if (!restoreResult.IsSuccess)
                return BuildResult.Failure(GenerateResult, restoreResult.AllInformation);

            var buildNoRestoreResult = await DotNetCliCommandExecutor.ExecuteAsync(
                WithArguments(GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, $"{Arguments} --no-restore", "build-no-restore")),
                cancellationToken).ConfigureAwait(false);
            return buildNoRestoreResult.ToBuildResult(GenerateResult);
        }

        [PublicAPI]
        public async Task<BuildResult> BuildAsync(CancellationToken cancellationToken = default)
        {
            DotNetCliCommandExecutor.LogEnvVars(WithArguments(""));

            if (BuildPartition.ForcedNoDependenciesForIntegrationTests)
            {
                // On our CI, Integration tests take too much time, because each benchmark run rebuilds BenchmarkDotNet itself.
                // To reduce the total duration of the CI workflows, we build all the projects without dependencies
                var result = await DotNetCliCommandExecutor.ExecuteAsync(
                    WithArguments(GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, $"{Arguments} --no-dependencies", "build-no-deps")),
                    cancellationToken).ConfigureAwait(false);
                return result.ToBuildResult(GenerateResult);
            }
            else
            {
                var result = await DotNetCliCommandExecutor.ExecuteAsync(
                    WithArguments(GetBuildCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, Arguments, "build")),
                    cancellationToken).ConfigureAwait(false);
                return result.ToBuildResult(GenerateResult);
            }
        }

        [PublicAPI]
        public async Task<BuildResult> PublishAsync(CancellationToken cancellationToken = default)
        {
            DotNetCliCommandExecutor.LogEnvVars(WithArguments(""));

            if (BuildPartition.ForcedNoDependenciesForIntegrationTests)
            {
                // On our CI, Integration tests take too much time, because each benchmark run rebuilds BenchmarkDotNet itself.
                // To reduce the total duration of the CI workflows, we build all the projects without dependencies
                var result = await DotNetCliCommandExecutor.ExecuteAsync(
                    WithArguments(GetPublishCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, $"{Arguments} --no-dependencies", "publish-no-deps")),
                    cancellationToken).ConfigureAwait(false);
                return result.ToBuildResult(GenerateResult);
            }
            else
            {
                var result = await DotNetCliCommandExecutor.ExecuteAsync(
                    WithArguments(GetPublishCommand(GenerateResult.ArtifactsPaths, BuildPartition, FilePath, TargetFrameworkMoniker, Arguments, "publish")),
                    cancellationToken).ConfigureAwait(false);
                return result.ToBuildResult(GenerateResult);
            }
        }

        internal static string GetRestoreCommand(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, string filePath, string? extraArguments = null, string? binLogSuffix = null)
            => new StringBuilder(256)
                .AppendArgument("restore")
                .AppendArgument(filePath.ToRelativePath(artifactsPaths).QuoteIfNeeded())
                // restore doesn't support -f argument.
                .AppendArgument(GetArtifactsPathArguments(artifactsPaths, buildPartition))
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
                .AppendArgument(GetArtifactsPathArguments(artifactsPaths, buildPartition))
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
                .AppendArgument(GetArtifactsPathArguments(artifactsPaths, buildPartition))
                .AppendArgument(GetCustomMsBuildArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver))
                .AppendArgument(extraArguments)
                .AppendArgument(GetMandatoryMsBuildSettings(buildPartition.BuildConfiguration))
                .AppendArgument(artifactsPaths.PackagesDirectoryName.IsBlank() ? string.Empty : $"/p:NuGetPackageRoot={artifactsPaths.PackagesDirectoryName.QuoteIfNeeded()}")
                .AppendArgument(GetMsBuildBinLogArgument(buildPartition, filePath, binLogSuffix))
                .ToString();

        private static string GetArtifactsPathArguments(ArtifactsPaths artifactsPaths, BuildPartition buildPartition)
        {
            // ArtifactsPath is a global property, so it relocates the referenced benchmark project's
            // output too. Integration tests build without dependencies and consume the output the test run
            // already produced, so the reference would resolve to a path nothing ever wrote.
            if (buildPartition.ForcedNoDependenciesForIntegrationTests)
                return "";

            // Absolute, because a relative one is resolved against each project's own directory rather than
            // the working directory: the benchmark project would then keep its intermediate output in its own
            // source tree, and every partition would share it, which is the sharing this is here to avoid.
            // A subdirectory, so that DefaultItemExcludes (which the SDK sets to $(ArtifactsPath)/**) doesn't
            // cover project-level files like wwwroot/.
            var artifactsPath = $"{artifactsPaths.BuildArtifactsDirectoryPath}{Path.AltDirectorySeparatorChar}.artifacts{Path.AltDirectorySeparatorChar}";

            // Set as a property rather than --artifacts-path, which is a .NET 8 SDK argument: an older SDK
            // errors on the argument but simply ignores the property. Those partitions get no isolated
            // intermediate output, which is why BenchmarkRunnerClean builds them one at a time.
            return $"/p:ArtifactsPath={artifactsPath.QuoteIfNeeded()}";
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
            if (path.StartsWith(buildArtifactsDirectoryPath, StringComparison.Ordinal))
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
