using System;
using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public abstract class DotNetCliGenerator : GeneratorBase
    {
        protected string TargetFrameworkMoniker { get; }

        protected Func<Platform, string> PlatformProvider { get; }

        protected string Runtime { get; }

        protected string ExtraDependencies { get; }

        protected string Imports { get; }

        [PublicAPI]
        protected DotNetCliGenerator(
            string targetFrameworkMoniker,
            string extraDependencies,
            Func<Platform, string> platformProvider,
            string imports,
            string runtime = null)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            ExtraDependencies = extraDependencies;
            PlatformProvider = platformProvider;
            Imports = imports;
            Runtime = runtime;
        }

        /// <summary>
        /// we need our folder to be on the same level as the project that we want to reference
        /// we are limited by xprojs (by default compiles all .cs files in all subfolders, Program.cs could be doubled and fail the build)
        /// and also by nuget internal implementation like looking for global.json file in parent folders
        /// </summary>
        protected override string GetBuildArtifactsDirectoryPath(Benchmark benchmark, string programName)
        {
            if (GetSolutionRootDirectory(out var directoryInfo))
            {
                return Path.Combine(directoryInfo.FullName, programName);
            }

            // we did not find global.json or any Visual Studio solution file? 
            // let's return it in the old way and hope that it works ;)
            return Path.Combine(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.FullName, programName);
        }

        protected bool GetSolutionRootDirectory(out DirectoryInfo directoryInfo)
        {
            directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directoryInfo != null)
            {
                if (IsRootSolutionFolder(directoryInfo))
                {
                    return true;
                }

                directoryInfo = directoryInfo.Parent;
            }

            return false;
        }

        /// <summary>
        /// we use custom output path in order to avoid any future problems related to dotnet cli ArtifactsPaths changes
        /// </summary>
        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", DotNetCliBuilder.Configuration, TargetFrameworkMoniker);

        protected override void Cleanup(ArtifactsPaths artifactsPaths)
        {
            if (!Directory.Exists(artifactsPaths.BuildArtifactsDirectoryPath))
            {
                return;
            }

            int attempt = 0;
            while (true)
            {
                try
                {
                    Directory.Delete(artifactsPaths.BuildArtifactsDirectoryPath, recursive: true);
                    return;
                }
                catch (DirectoryNotFoundException) // it's crazy but it happens ;)
                {
                    return;
                }
                catch (Exception) when (attempt++ < 5)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(1000)); // Previous benchmark run didn't release some files
                }
            }
        }

        protected override void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths)
        {
            if (!Directory.Exists(artifactsPaths.BinariesDirectoryPath))
            {
                Directory.CreateDirectory(artifactsPaths.BinariesDirectoryPath);
            }
        }

        protected override void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
        {
            string content = $"call dotnet {DotNetCliBuilder.RestoreCommand}{Environment.NewLine}" +
                             $"call dotnet {DotNetCliBuilder.GetBuildCommand(TargetFrameworkMoniker, justTheProjectItself: false)}";

            File.WriteAllText(artifactsPaths.BuildScriptFilePath, content);
        }

        protected static string SetPlatform(string template, string platform) => template.Replace("$PLATFORM$", platform);

        protected static string SetCodeFileName(string template, string codeFileName) => template.Replace("$CODEFILENAME$", codeFileName);

        protected static string SetTargetFrameworkMoniker(string content, string targetFrameworkMoniker) => content.Replace("$TFM$", targetFrameworkMoniker);

        private static bool IsRootSolutionFolder(DirectoryInfo directoryInfo)
            => directoryInfo
                .GetFileSystemInfos()
                .Any(fileInfo => fileInfo.Extension == ".sln" || fileInfo.Name == "global.json");
    }
}