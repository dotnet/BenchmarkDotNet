using BenchmarkDotNet.Running;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    public abstract class DotNetCliGenerator(DotNetCliSettings settings, bool isNetCore) : GeneratorBase
    {
        private static readonly string[] ProjectExtensions = [".csproj", ".fsproj", ".vbroj"];

        private static readonly string[] SolutionExtensions = [".sln", ".slnx"];

        /// <summary>
        /// The settings the toolchain built this generator with. The target framework moniker is already resolved
        /// against the runtime (never blank).
        /// </summary>
        public DotNetCliSettings Settings => settings;

        protected bool IsNetCore => isNetCore;

        protected override string GetExecutableExtension() => IsNetCore ? ".dll" : ".exe";

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
            string cli = Settings.CliPath?.FullName ?? DotNetCliCommandExecutor.DefaultDotNetCliPath.Value;
            var content = new StringBuilder(300)
                .AppendLine($"call {cli} {DotNetCliCommand.GetRestoreCommand(artifactsPaths, buildPartition, artifactsPaths.ProjectFilePath)}")
                .AppendLine($"call {cli} {DotNetCliCommand.GetBuildCommand(artifactsPaths, buildPartition, artifactsPaths.ProjectFilePath, Settings.TargetFrameworkMoniker)}")
                .ToString();

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
