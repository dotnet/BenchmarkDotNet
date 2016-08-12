using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    internal class DotNetCliGenerator : GeneratorBase
    {
        private string TargetFrameworkMoniker { get; }

        private string ExtraDependencies { get; }

        private Func<Platform, string> PlatformProvider { get; }

        private string Imports { get; }

        private string Runtime { get; }

        public DotNetCliGenerator(
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
            var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directoryInfo != null)
            {
                if (IsRootSolutionFolder(directoryInfo))
                {
                    return Path.Combine(directoryInfo.FullName, programName);
                }

                directoryInfo = directoryInfo.Parent;
            }

            // we did not find global.json or any Visual Studio solution file? 
            // let's return it in the old way and hope that it works ;)
            return Path.Combine(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.FullName, programName);
        }

        /// <summary>
        /// we use custom output path in order to avoid any future problems related to dotnet cli ArtifactsPaths changes
        /// </summary>
        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath)
            => Path.Combine(buildArtifactsDirectoryPath, DotNetCliBuilder.OutputDirectory);

        protected override string GetProjectFilePath(string binariesDirectoryPath)
            => Path.Combine(binariesDirectoryPath, "project.json");

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
                    Thread.Sleep(TimeSpan.FromMilliseconds(500)); // Previous benchmark run didn't release some files
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

        protected override void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            var template = ResourceHelper.LoadTemplate("BenchmarkProject.json");

            var content = SetPlatform(template, PlatformProvider(benchmark.Job.Platform));
            content = SetCodeFileName(content, Path.GetFileName(artifactsPaths.ProgramCodePath));
            content = SetDependencyToExecutingAssembly(content, benchmark.Target.Type);
            content = SetTargetFrameworkMoniker(content, TargetFrameworkMoniker);
            content = SetExtraDependencies(content, ExtraDependencies);
            content = SetImports(content, Imports);
            content = SetRuntime(content, Runtime);
            content = SetGcMode(content, benchmark.Job.GcMode);

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);
        }

        protected override void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            var content = $"call dotnet {DotNetCliBuilder.RestoreCommand}{Environment.NewLine}" +
                          $"call dotnet {DotNetCliBuilder.GetBuildCommand(TargetFrameworkMoniker)}";

            File.WriteAllText(artifactsPaths.BuildScriptFilePath, content);
        }

        private string SetPlatform(string template, string platform) => template.Replace("$PLATFORM$", platform);

        private string SetCodeFileName(string template, string codeFileName) => template.Replace("$CODEFILENAME$", codeFileName);

        private string SetDependencyToExecutingAssembly(string template, Type benchmarkTarget)
        {
            var assemblyName = benchmarkTarget.GetTypeInfo().Assembly.GetName();
            var packageVersion = GetPackageVersion(assemblyName);

            return template
                .Replace("$EXECUTINGASSEMBLYVERSION$", packageVersion)
                .Replace("$EXECUTINGASSEMBLY$", assemblyName.Name);
        }

        private string SetTargetFrameworkMoniker(string content, string targetFrameworkMoniker) => content.Replace("$TFM$", targetFrameworkMoniker);

        private string SetExtraDependencies(string content, string extraDependencies) => content.Replace("$REQUIREDDEPENDENCY$", extraDependencies);

        private string SetImports(string content, string imports) => content.Replace("$IMPORTS$", imports);

        private string SetRuntime(string content, string runtime) => content.Replace("$RUNTIME$", runtime);

        private string SetGcMode(string content, GcMode gcMode)
        {
            if (gcMode == null || gcMode == GcMode.Default)
            {
                return content.Replace("$GC$", null);
            }

            return content.Replace(
                "$GC$",
                $"\"runtimeOptions\": {{ \"configProperties\": {{ \"System.GC.Concurrent\": {gcMode.Concurrent.ToString().ToLower()}, \"System.GC.Server\": {gcMode.Server.ToString().ToLower()} }} }}, ");
        }

        /// <summary>
        /// we can not simply call assemblyName.Version.ToString() because it is different than package version which can contain (and often does) text
        /// we are using the wildcard to get latest version of package/project restored
        /// </summary>
        private static string GetPackageVersion(AssemblyName assemblyName)
        {
            return $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}-*";
        }

        private static bool IsRootSolutionFolder(DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
            {
                return false;
            }

            return directoryInfo
                .GetFileSystemInfos()
                .Any(fileInfo => fileInfo.Extension == "sln" || fileInfo.Name == "global.json");
        }
    }
}