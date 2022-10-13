using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;
using StreamWriter = System.IO.StreamWriter;

namespace BenchmarkDotNet.Toolchains
{
    [PublicAPI]
    public abstract class GeneratorBase : IGenerator
    {
        public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
        {
            var artifactsPaths = ArtifactsPaths.Empty;
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

        /// <summary>
        /// returns a path to the folder where auto-generated project and code are going to be placed
        /// </summary>
        [PublicAPI] protected abstract string GetBuildArtifactsDirectoryPath(BuildPartition assemblyLocation, string programName);

        /// <summary>
        /// returns a path where executable should be found after the build
        /// </summary>
        [PublicAPI] protected virtual string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => buildArtifactsDirectoryPath;

        /// <summary>
        /// returns OS-specific executable extension
        /// </summary>
        [PublicAPI] protected virtual string GetExecutableExtension()
            => RuntimeInformation.ExecutableExtension;

        /// <summary>
        /// returns a path to the auto-generated .csproj file
        /// </summary>
        [PublicAPI] protected virtual string GetProjectFilePath(string buildArtifactsDirectoryPath)
            => string.Empty;

        /// <summary>
        /// returns a list of artifacts that should be removed after running the benchmarks
        /// </summary>
        [PublicAPI] protected abstract string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths);

        /// <summary>
        /// if you need to copy some extra files to make the benchmarks work you should override this method
        /// </summary>
        [PublicAPI] protected virtual void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths) { }

        /// <summary>
        /// generates NuGet.Config file to make sure that BDN is using the right NuGet feeds
        /// </summary>
        [PublicAPI] protected virtual void GenerateNuGetConfig(ArtifactsPaths artifactsPaths) { }

        /// <summary>
        /// generates .csproj file with a reference to the project with benchmarks
        /// </summary>
        [PublicAPI] protected virtual void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger) { }

        /// <summary>
        /// generates a script can be used when debugging compilation issues
        /// </summary>
        [PublicAPI] protected abstract void GenerateBuildScript(BuildPartition buildPartition, ArtifactsPaths artifactsPaths);

        /// <summary>
        /// returns a path to the folder where NuGet packages should be restored
        /// </summary>
        [PublicAPI] protected virtual string GetPackagesDirectoryPath(string buildArtifactsDirectoryPath) => default;

        /// <summary>
        /// generates an app.config file next to the executable with benchmarks
        /// </summary>
        [PublicAPI] protected virtual void GenerateAppConfig(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
        {
            string sourcePath = buildPartition.AssemblyLocation + ".config";

            using (var source = File.Exists(sourcePath) ? new StreamReader(File.OpenRead(sourcePath)) : TextReader.Null)
            using (var destination = new StreamWriter(File.Create(artifactsPaths.AppConfigPath), Encoding.UTF8))
            {
                AppConfigGenerator.Generate(buildPartition.RepresentativeBenchmarkCase.Job, source, destination, buildPartition.Resolver);
            }
        }

        /// <summary>
        /// generates the C# source code with all required boilerplate.
        /// <remarks>You most probably do NOT need to override this method!!</remarks>
        /// </summary>
        [PublicAPI] protected virtual void GenerateCode(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
            => File.WriteAllText(artifactsPaths.ProgramCodePath, CodeGenerator.Generate(buildPartition));

        protected virtual string GetExecutablePath(string binariesDirectoryPath, string programName) => Path.Combine(binariesDirectoryPath, $"{programName}{GetExecutableExtension()}");

        private ArtifactsPaths GetArtifactsPaths(BuildPartition buildPartition, string rootArtifactsFolderPath)
        {
            // its not ".cs" in order to avoid VS from displaying and compiling it with xprojs/csprojs that include all *.cs by default
            const string codeFileExtension = ".notcs";

            string programName = buildPartition.ProgramName;
            string buildArtifactsDirectoryPath = GetBuildArtifactsDirectoryPath(buildPartition, programName);
            string binariesDirectoryPath = GetBinariesDirectoryPath(buildArtifactsDirectoryPath, buildPartition.BuildConfiguration);

            string executablePath = GetExecutablePath(binariesDirectoryPath, programName);

            return new ArtifactsPaths(
                rootArtifactsFolderPath: rootArtifactsFolderPath,
                buildArtifactsDirectoryPath: buildArtifactsDirectoryPath,
                binariesDirectoryPath: binariesDirectoryPath,
                programCodePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{codeFileExtension}"),
                appConfigPath: $"{executablePath}.config",
                nuGetConfigPath: Path.Combine(buildArtifactsDirectoryPath, "NuGet.config"),
                projectFilePath: GetProjectFilePath(buildArtifactsDirectoryPath),
                buildScriptFilePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{RuntimeInformation.ScriptFileExtension}"),
                executablePath: executablePath,
                programName: programName,
                packagesDirectoryName: GetPackagesDirectoryPath(buildArtifactsDirectoryPath));
        }
    }
}