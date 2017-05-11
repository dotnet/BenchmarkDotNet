﻿using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    public abstract class GeneratorBase : IGenerator
    {
        public GenerateResult GenerateProject(Benchmark benchmark, ILogger logger, string rootArtifactsFolderPath, IConfig config, IResolver resolver)
        {
            ArtifactsPaths artifactsPaths = null;
            try
            {
                artifactsPaths = GetArtifactsPaths(benchmark, config, rootArtifactsFolderPath);

                Cleanup(benchmark, artifactsPaths);

                CopyAllRequiredFiles(benchmark, artifactsPaths);

                GenerateCode(benchmark, artifactsPaths);
                GenerateAppConfig(benchmark, artifactsPaths, resolver);
                GenerateProject(benchmark, artifactsPaths, resolver, logger);
                GenerateBuildScript(benchmark, artifactsPaths, resolver);

                return GenerateResult.Success(artifactsPaths);
            }
            catch (Exception ex)
            {
                return GenerateResult.Failure(artifactsPaths, ex);
            }
        }

        protected abstract string GetBuildArtifactsDirectoryPath(Benchmark benchmark, string programName);

        protected virtual string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath) => buildArtifactsDirectoryPath;

        protected virtual string GetProjectFilePath(string binariesDirectoryPath) => string.Empty;

        protected abstract void Cleanup(Benchmark benchmark, ArtifactsPaths artifactsPaths);

        protected virtual void CopyAllRequiredFiles(Benchmark benchmark, ArtifactsPaths artifactsPaths) { }

        protected virtual void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver, ILogger logger) { }

        protected abstract void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver);

        private ArtifactsPaths GetArtifactsPaths(Benchmark benchmark, IConfig config, string rootArtifactsFolderPath)
        {
            // its not ".cs" in order to avoid VS from displaying and compiling it with xprojs
            const string codeFileExtension = ".notcs";

            string programName = GetProgramName(benchmark, config);
            string buildArtifactsDirectoryPath = GetBuildArtifactsDirectoryPath(benchmark, programName);
            string binariesDirectoryPath = GetBinariesDirectoryPath(buildArtifactsDirectoryPath);
            string executablePath = Path.Combine(binariesDirectoryPath, $"{programName}{ServicesProvider.RuntimeInformation.ExecutableExtension}");

            return new ArtifactsPaths(
                cleanup: artifactsPaths => Cleanup(benchmark, artifactsPaths),
                rootArtifactsFolderPath: rootArtifactsFolderPath,
                buildArtifactsDirectoryPath: buildArtifactsDirectoryPath,
                binariesDirectoryPath: binariesDirectoryPath,
                programCodePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{codeFileExtension}"),
                appConfigPath: $"{executablePath}.config",
                projectFilePath: GetProjectFilePath(buildArtifactsDirectoryPath),
                buildScriptFilePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{ServicesProvider.RuntimeInformation.ScriptFileExtension}"),
                executablePath: executablePath,
                programName: programName);
        }

        /// <summary>
        /// when config is set to KeepBenchmarkFiles we use benchmark.ShortInfo as name,
        /// otherwise (default) "BDN.Generated", mostly to prevent PathTooLongException
        /// </summary>
        private static string GetProgramName(Benchmark benchmark, IConfig config)
        {
            const string shortName = "BDN.Generated";
            return config.KeepBenchmarkFiles ? benchmark.FolderInfo : shortName;
        }

        protected virtual void GenerateCode(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            File.WriteAllText(artifactsPaths.ProgramCodePath, CodeGenerator.Generate(benchmark));
        }

        private static void GenerateAppConfig(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
        {
            string configFilePath = ServicesProvider.DotNetStandardWorkarounds.GetLocation(benchmark.Target.Type.GetTypeInfo().Assembly) + ".config";

            if (!File.Exists(configFilePath)) // there might be other dll that executes benchmarks, has binding redirects and does not define benchmarks
            {
                // we try to search for other config files, which might contain precious assembly binding redirects 
                var allConfigs = new DirectoryInfo(
                        Path.GetDirectoryName(ServicesProvider.DotNetStandardWorkarounds.GetLocation(benchmark.Target.Type.GetTypeInfo().Assembly)))
                    .GetFiles("*.config");

                if (allConfigs.Length == 1) // if there is only one config, we will use it
                    configFilePath = allConfigs[0].FullName;
            }

            using (var source = File.Exists(configFilePath) ? new StreamReader(File.OpenRead(configFilePath)) : TextReader.Null)
            using (var destination = new System.IO.StreamWriter(File.Create(artifactsPaths.AppConfigPath), System.Text.Encoding.UTF8))
            {
                AppConfigGenerator.Generate(benchmark.Job, source, destination, resolver);
            }
        }
    }
}
