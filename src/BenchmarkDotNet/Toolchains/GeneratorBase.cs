using System;
using System.IO;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains
{
    public abstract class GeneratorBase : IGenerator
    {
        public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
        {
            ArtifactsPaths artifactsPaths = ArtifactsPaths.Empty;
            try
            {
                artifactsPaths = GetArtifactsPaths(buildPartition, rootArtifactsFolderPath);

                CopyAllRequiredFiles(artifactsPaths);

                GenerateCode(buildPartition, artifactsPaths);
                GenerateAppConfig(buildPartition, artifactsPaths);
                GenerateNuGetConfig(artifactsPaths);
                GenerateProject(buildPartition, artifactsPaths, logger);
                GenerateBuildScript(buildPartition, artifactsPaths);

                return GenerateResult.Success(artifactsPaths, GetArtifactsToCleanup(artifactsPaths));
            }
            catch (Exception ex)
            {
                return GenerateResult.Failure(artifactsPaths, GetArtifactsToCleanup(artifactsPaths), ex);
            }
        }

        protected abstract string GetBuildArtifactsDirectoryPath(BuildPartition assemblyLocation, string programName);

        protected virtual string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration) => buildArtifactsDirectoryPath;

        protected virtual string GetExecutableExtension() => RuntimeInformation.ExecutableExtension;

        protected virtual string GetProjectFilePath(string binariesDirectoryPath) => string.Empty;

        protected abstract string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths);

        protected virtual void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths) { }

        protected virtual void GenerateNuGetConfig(ArtifactsPaths artifactsPaths) { }

        protected virtual void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger) { }

        protected abstract void GenerateBuildScript(BuildPartition buildPartition, ArtifactsPaths artifactsPaths);

        protected virtual string GetPackagesDirectoryPath(string buildArtifactsDirectoryPath) => Path.Combine(buildArtifactsDirectoryPath, "packages");

        private ArtifactsPaths GetArtifactsPaths(BuildPartition buildPartition, string rootArtifactsFolderPath)
        {
            // its not ".cs" in order to avoid VS from displaying and compiling it with xprojs/csprojs that include all *.cs by default
            const string codeFileExtension = ".notcs";

            string programName = buildPartition.ProgramName;
            string buildArtifactsDirectoryPath = GetBuildArtifactsDirectoryPath(buildPartition, programName);
            string binariesDirectoryPath = GetBinariesDirectoryPath(buildArtifactsDirectoryPath, buildPartition.BuildConfiguration);
            string executablePath = Path.Combine(binariesDirectoryPath, $"{programName}{GetExecutableExtension()}");

            return new ArtifactsPaths(
                rootArtifactsFolderPath: rootArtifactsFolderPath,
                buildArtifactsDirectoryPath: buildArtifactsDirectoryPath,
                binariesDirectoryPath: binariesDirectoryPath,
                programCodePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{codeFileExtension}"),
                appConfigPath: $"{executablePath}.config",
                nugetConfigPath: Path.Combine(buildArtifactsDirectoryPath, "NuGet.config"),
                projectFilePath: GetProjectFilePath(buildArtifactsDirectoryPath),
                buildScriptFilePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{RuntimeInformation.ScriptFileExtension}"),
                executablePath: executablePath,
                programName: programName,
                packagesDirectoryName: GetPackagesDirectoryPath(buildArtifactsDirectoryPath));
        }

        private static void GenerateCode(BuildPartition buildPartition, ArtifactsPaths artifactsPaths) 
            => File.WriteAllText(artifactsPaths.ProgramCodePath, CodeGenerator.Generate(buildPartition));

        private static void GenerateAppConfig(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
        {
            string sourcePath = buildPartition.AssemblyLocation + ".config";

            using (var source = File.Exists(sourcePath) ? new StreamReader(File.OpenRead(sourcePath)) : TextReader.Null)
            using (var destination = new System.IO.StreamWriter(File.Create(artifactsPaths.AppConfigPath), System.Text.Encoding.UTF8))
            {
                AppConfigGenerator.Generate(buildPartition.RepresentativeBenchmark.Job, source, destination, buildPartition.Resolver);
            }
        }
    }
}