using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public abstract class DotNetCliGenerator : GeneratorBase
    {
        internal string TargetFrameworkMoniker { get; }

        protected Func<Platform, string> PlatformProvider { get; }

        protected string Runtime { get; }

        protected string ExtraDependencies { get; }

        protected string Imports { get; }

        private DotNetCliBuilder Builder { get; }

        [PublicAPI]
        protected DotNetCliGenerator(
            DotNetCliBuilder builder,
            string targetFrameworkMoniker,
            string extraDependencies,
            Func<Platform, string> platformProvider,
            string imports,
            string runtime = null)
        {
            Builder = builder;
            TargetFrameworkMoniker = targetFrameworkMoniker;
            ExtraDependencies = extraDependencies;
            PlatformProvider = platformProvider;
            Imports = imports;
            Runtime = runtime;
        }

        protected override string GetExecutableExtension() => TargetFrameworkMoniker.Contains("core") ? ".dll" : ".exe";

        /// <summary>
        /// we need our folder to be on the same level as the project that we want to reference
        /// we are limited by xprojs (by default compiles all .cs files in all subfolders, Program.cs could be doubled and fail the build)
        /// and also by nuget internal implementation like looking for global.json file in parent folders
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

        protected override string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths)
            => new[] { artifactsPaths.BuildArtifactsDirectoryPath };

        protected override void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths)
        {
            if (!Directory.Exists(artifactsPaths.BinariesDirectoryPath))
            {
                Directory.CreateDirectory(artifactsPaths.BinariesDirectoryPath);
            }
        }

        protected override void GenerateBuildScript(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
        {
            string content = $"call dotnet {Builder.RestoreCommand} {GetCustomArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver)}{Environment.NewLine}" +
                             $"call dotnet {Builder.GetBuildCommand(TargetFrameworkMoniker, false, buildPartition.BuildConfiguration)} {GetCustomArguments(buildPartition.RepresentativeBenchmarkCase, buildPartition.Resolver)}";

            File.WriteAllText(artifactsPaths.BuildScriptFilePath, content);
        }

        protected static string SetPlatform(string template, string platform) => template.Replace("$PLATFORM$", platform);

        protected static string SetCodeFileName(string template, string codeFileName) => template.Replace("$CODEFILENAME$", codeFileName);

        protected static string SetTargetFrameworkMoniker(string content, string targetFrameworkMoniker) => content.Replace("$TFM$", targetFrameworkMoniker);

        private static bool IsRootSolutionFolder(DirectoryInfo directoryInfo)
            => directoryInfo
                .GetFileSystemInfos()
                .Any(fileInfo => fileInfo.Extension == ".sln" || fileInfo.Name == "global.json");

        internal static string GetCustomArguments(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (!benchmarkCase.Job.HasValue(InfrastructureMode.ArgumentsCharacteristic))
                return null;

            var msBuildArguments = benchmarkCase.Job.ResolveValue(InfrastructureMode.ArgumentsCharacteristic, resolver).OfType<MsBuildArgument>();

            return string.Join(" ", msBuildArguments.Select(arg => arg.TextRepresentation));
        }
    }
}