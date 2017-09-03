using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
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
                artifactsPaths = GetArtifactsPaths(benchmark, config, rootArtifactsFolderPath, resolver);

                CopyAllRequiredFiles(benchmark, artifactsPaths);

                GenerateCode(benchmark, artifactsPaths);
                GenerateAppConfig(benchmark, artifactsPaths, resolver);
                GenerateProject(benchmark, artifactsPaths, resolver, logger);
                GenerateBuildScript(benchmark, artifactsPaths, resolver);

                return GenerateResult.Success(artifactsPaths, GetArtifactsToCleanup(benchmark, artifactsPaths));
            }
            catch (Exception ex)
            {
                return GenerateResult.Failure(artifactsPaths, GetArtifactsToCleanup(benchmark, artifactsPaths), ex);
            }
        }

        protected abstract string GetBuildArtifactsDirectoryPath(Benchmark benchmark, string programName);

        protected virtual string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration) 
            => buildArtifactsDirectoryPath;

        protected virtual string GetProjectFilePath(string binariesDirectoryPath) => string.Empty;

        protected abstract string[] GetArtifactsToCleanup(Benchmark benchmark, ArtifactsPaths artifactsPaths);

        protected virtual void CopyAllRequiredFiles(Benchmark benchmark, ArtifactsPaths artifactsPaths) { }

        protected virtual void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver, ILogger logger) { }

        protected abstract void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver);

        private ArtifactsPaths GetArtifactsPaths(Benchmark benchmark, IConfig config, string rootArtifactsFolderPath, IResolver resolver)
        {
            // its not ".cs" in order to avoid VS from displaying and compiling it with xprojs/csprojs that include all *.cs by default
            const string codeFileExtension = ".notcs";

            string programName = GetProgramName(benchmark, config);
            string buildArtifactsDirectoryPath = GetBuildArtifactsDirectoryPath(benchmark, programName);
            string binariesDirectoryPath = GetBinariesDirectoryPath(buildArtifactsDirectoryPath, benchmark.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, resolver));
            string executablePath = Path.Combine(binariesDirectoryPath, $"{programName}{RuntimeInformation.ExecutableExtension}");

            return new ArtifactsPaths(
                rootArtifactsFolderPath: rootArtifactsFolderPath,
                buildArtifactsDirectoryPath: buildArtifactsDirectoryPath,
                binariesDirectoryPath: binariesDirectoryPath,
                programCodePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{codeFileExtension}"),
                appConfigPath: $"{executablePath}.config",
                projectFilePath: GetProjectFilePath(buildArtifactsDirectoryPath),
                buildScriptFilePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{RuntimeInformation.ScriptFileExtension}"),
                executablePath: executablePath,
                programName: programName);
        }

        /// <summary>
        /// when config is set to KeepBenchmarkFiles we use benchmark.ShortInfo as name (some human might want to use it's content),
        /// otherwise (default) new Guid to reduce the chance for UnauthorizedAccessException
        /// </summary>
        private static string GetProgramName(Benchmark benchmark, IConfig config)
            => config.KeepBenchmarkFiles ? benchmark.FolderInfo : Guid.NewGuid().ToString();

        private static void GenerateCode(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            File.WriteAllText(artifactsPaths.ProgramCodePath, CodeGenerator.Generate(benchmark));
        }

        private static void GenerateAppConfig(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
        {
            string sourcePath = benchmark.Target.Type.GetTypeInfo().Assembly.Location + ".config";

            using (var source = File.Exists(sourcePath) ? new StreamReader(File.OpenRead(sourcePath)) : TextReader.Null)
            using (var destination = new System.IO.StreamWriter(File.Create(artifactsPaths.AppConfigPath), System.Text.Encoding.UTF8))
            {
                AppConfigGenerator.Generate(benchmark.Job, source, destination, resolver);
            }
        }
    }
}