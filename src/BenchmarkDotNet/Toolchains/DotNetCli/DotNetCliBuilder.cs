using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public abstract class DotNetCliBuilder(DotNetCliSettings settings) : BuilderBase
    {
        private static readonly string[] ProjectExtensions = [".csproj", ".fsproj", ".vbroj"];

        private static readonly string[] SolutionExtensions = [".sln", ".slnx"];

        /// <summary>
        /// The settings the toolchain built this builder with. The target framework moniker is already resolved
        /// against the runtime (never blank).
        /// </summary>
        public DotNetCliSettings Settings => settings;

        /// <summary>
        /// Whether the toolchain publishes the project after building it, rather than only building it.
        /// </summary>
        protected virtual bool PublishesOutput => false;

        /// <summary>
        /// Extra arguments appended to every cli command.
        /// </summary>
        protected virtual string? ExtraArguments => null;

        /// <summary>
        /// Environment variables set for every cli command.
        /// </summary>
        protected virtual IReadOnlyList<EnvironmentVariable> EnvironmentVariables => [];

        /// <summary>
        /// Whether the cli output is written to the logger even when the build succeeds.
        /// </summary>
        protected virtual bool LogOutput => false;

        /// <summary>
        /// The file handed to the dotnet cli. It is the generated project by default; a toolchain that builds a
        /// standalone source file rather than a project returns that file instead.
        /// </summary>
        protected virtual string GetBuildFilePath(ArtifactsPaths artifactsPaths) => artifactsPaths.ProjectFilePath;

        // .Net SDK 8+ supports ArtifactsPath for proper parallel builds.
        // Older SDKs may produce builds with incorrect bindings if more than 1 partition is built concurrently.
        public override bool GetSupportsConcurrency(BuildPartition buildPartition)
            => buildPartition.RepresentativeBenchmarkCase.GetRuntime().Version?.Major >= 8;

        protected override async ValueTask<BuildResult> BuildAsync(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
        {
            var buildResult = await RunDotNetCliAsync(
                new DotNetCliCommand(
                    Settings.CliPath,
                    GetBuildFilePath(artifactsPaths),
                    Settings.TargetFrameworkMoniker,
                    ExtraArguments,
                    artifactsPaths,
                    logger,
                    buildPartition,
                    EnvironmentVariables,
                    buildPartition.Timeout,
                    logOutput: LogOutput
                ),
                cancellationToken
            ).ConfigureAwait(false);

            if (!PublishesOutput && buildResult.IsBuildSuccess && buildPartition.RepresentativeBenchmarkCase.Job.Environment.LargeAddressAware)
            {
                LargeAddressAware.SetLargeAddressAware(artifactsPaths.ExecutablePath);
            }

            return buildResult;
        }

        /// <summary>
        /// Runs the dotnet cli command that produces the executable. Both shapes restore in the same invocation.
        /// </summary>
        protected virtual Task<BuildResult> RunDotNetCliAsync(DotNetCliCommand command, CancellationToken cancellationToken)
            => PublishesOutput
                ? command.PublishAsync(cancellationToken)
                : command.BuildAsync(cancellationToken);

        protected override string GetExecutableExtension() => ".dll";

        /// <summary>
        /// we need our folder to be on the same level as the project that we want to reference
        /// we are limited by xprojs (by default compiles all .cs files in all subfolders, Program.cs could be doubled and fail the build)
        /// and also by NuGet internal implementation like looking for global.json file in parent folders
        /// </summary>
        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
        {
            if (GetSolutionRootDirectory(out var directoryInfo))
            {
                return Path.Combine(directoryInfo.FullName, programName);
            }

            // we did not find global.json or any Visual Studio solution file?
            // let's return it in the old way and hope that it works ;)
            var parent = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent;
            if (parent == null)
                throw new DirectoryNotFoundException("Parent directory for current directory");
            return Path.Combine(parent.FullName, programName);
        }

        internal static bool GetSolutionRootDirectory([NotNullWhen(true)] out DirectoryInfo? directoryInfo)
        {
            return GetRootDirectory(IsRootSolutionFolder, out directoryInfo);
        }

        internal static bool GetProjectRootDirectory([NotNullWhen(true)] out DirectoryInfo? directoryInfo)
        {
            return GetRootDirectory(IsRootProjectFolder, out directoryInfo);
        }

        internal static bool GetRootDirectory(Func<DirectoryInfo, bool> condition, [NotNullWhen(true)] out DirectoryInfo? directoryInfo)
        {
            directoryInfo = null;
            try
            {
                directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (directoryInfo != null)
                {
                    if (condition(directoryInfo))
                    {
                        return true;
                    }

                    directoryInfo = directoryInfo.Parent;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        protected override string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths)
            => [artifactsPaths.BuildArtifactsDirectoryPath];

        protected override void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths)
        {
            if (!Directory.Exists(artifactsPaths.BinariesDirectoryPath))
            {
                Directory.CreateDirectory(artifactsPaths.BinariesDirectoryPath);
            }
        }

        protected override string GetPackagesDirectoryPath(string buildArtifactsDirectoryPath) => Settings.PackagesPath?.FullName ?? "";

        protected override ValueTask GenerateBuildScriptAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, CancellationToken cancellationToken)
        {
            // Mirrors RunDotNetCliAsync, whose two shapes both restore in the same invocation.
            string cli = Settings.CliPath?.FullName ?? DotNetCliCommandExecutor.DefaultDotNetCliPath.Value;
            string file = GetBuildFilePath(artifactsPaths);
            string tfm = Settings.TargetFrameworkMoniker;
            string command = PublishesOutput
                ? DotNetCliCommand.GetPublishCommand(artifactsPaths, buildPartition, file, tfm, ExtraArguments)
                : DotNetCliCommand.GetBuildCommand(artifactsPaths, buildPartition, file, tfm, ExtraArguments);

            var content = new StringBuilder(300).AppendLine($"call {cli} {command}").ToString();

            return new(File.WriteAllTextAsync(artifactsPaths.BuildScriptFilePath, content, cancellationToken));
        }

        private static bool IsRootSolutionFolder(DirectoryInfo directoryInfo)
            => directoryInfo
                .GetFileSystemInfos()
                .Any(fileInfo => SolutionExtensions.Contains(fileInfo.Extension) || fileInfo.Name == "global.json");

        private static bool IsRootProjectFolder(DirectoryInfo directoryInfo)
            => directoryInfo
                .GetFileSystemInfos()
                .Any(fileInfo => ProjectExtensions.Contains(fileInfo.Extension));
    }
}
