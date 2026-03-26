using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    public class InProcessEmitGenerator : IGenerator
    {
        public async ValueTask<GenerateResult> GenerateProjectAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken)
        {
            var artifactsPaths = ArtifactsPaths.Empty;
            try
            {
                artifactsPaths = GetArtifactsPaths(buildPartition, rootArtifactsFolderPath);

                return GenerateResult.Success(artifactsPaths, []);
            }
            catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, cancellationToken))
            {
                logger.WriteLineError($"Failed to generate partition: {ex}");
                return GenerateResult.Failure(artifactsPaths, []);
            }
        }

        private string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath) => buildArtifactsDirectoryPath;

        private string GetExecutableExtension() => ".dll";

        private string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition) => Path.GetDirectoryName(buildPartition.AssemblyLocation)!;

        private ArtifactsPaths GetArtifactsPaths(BuildPartition buildPartition, string rootArtifactsFolderPath)
        {
            string programName = buildPartition.ProgramName + RunnableConstants.DynamicAssemblySuffix;
            string buildArtifactsDirectoryPath = GetBuildArtifactsDirectoryPath(buildPartition);
            string binariesDirectoryPath =
                GetBinariesDirectoryPath(buildArtifactsDirectoryPath);
            string executablePath = Path.Combine(binariesDirectoryPath, $"{programName}{GetExecutableExtension()}");

            return new ArtifactsPaths(
                rootArtifactsFolderPath: rootArtifactsFolderPath,
                buildArtifactsDirectoryPath: buildArtifactsDirectoryPath,
                binariesDirectoryPath: binariesDirectoryPath,
                publishDirectoryPath: "",
                programCodePath: "",
                appConfigPath: "",
                nuGetConfigPath: "",
                projectFilePath: "",
                buildScriptFilePath: "",
                executablePath: executablePath,
                programName: programName,
                packagesDirectoryName: "");
        }
    }
}