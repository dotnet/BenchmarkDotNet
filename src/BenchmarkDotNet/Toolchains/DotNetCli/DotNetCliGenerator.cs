using System;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public abstract class DotNetCliGenerator : GeneratorBase
    {
        private static readonly string[] ProjectExtensions = { ".csproj", ".fsproj", ".vbroj" };

        [PublicAPI] public string TargetFrameworkMoniker { get; }

        [PublicAPI] public string CliPath { get; }

        [PublicAPI] public string PackagesPath { get; }

        protected bool IsNetCore { get; }

        [PublicAPI]
        protected DotNetCliGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, bool isNetCore)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            CliPath = cliPath;
            PackagesPath = packagesPath;
            IsNetCore = isNetCore;
        }

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

        internal static bool GetSolutionRootDirectory(out DirectoryInfo directoryInfo)
        {
            return GetRootDirectory(IsRootSolutionFolder, out directoryInfo);
        }

        internal static bool GetProjectRootDirectory(out DirectoryInfo directoryInfo)
        {
            return GetRootDirectory(IsRootProjectFolder, out directoryInfo);
        }

        internal static bool GetRootDirectory(Func<DirectoryInfo, bool> condition, out DirectoryInfo? directoryInfo)
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
            => new[] { artifactsPaths.BuildArtifactsDirectoryPath };

        protected override void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths)
        {
            if (!Directory.Exists(artifactsPaths.BinariesDirectoryPath))
            {
                Directory.CreateDirectory(artifactsPaths.BinariesDirectoryPath);
            }
        }

        protected override string GetPackagesDirectoryPath(string buildArtifactsDirectoryPath) => PackagesPath;

        protected override void GenerateBuildScript(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
        {
            var content = new StringBuilder(300)
                .AppendLine($"call {CliPath ?? "dotnet"} {DotNetCliCommand.GetRestoreCommand(artifactsPaths, buildPartition)}")
                .AppendLine($"call {CliPath ?? "dotnet"} {DotNetCliCommand.GetBuildCommand(artifactsPaths, buildPartition)}")
                .ToString();

            File.WriteAllText(artifactsPaths.BuildScriptFilePath, content);
        }

        private static bool IsRootSolutionFolder(DirectoryInfo directoryInfo)
            => directoryInfo
                .GetFileSystemInfos()
                .Any(fileInfo => fileInfo.Extension == ".sln" || fileInfo.Name == "global.json");

        private static bool IsRootProjectFolder(DirectoryInfo directoryInfo)
            => directoryInfo
                .GetFileSystemInfos()
                .Any(fileInfo => ProjectExtensions.Contains(fileInfo.Extension));
    }
}