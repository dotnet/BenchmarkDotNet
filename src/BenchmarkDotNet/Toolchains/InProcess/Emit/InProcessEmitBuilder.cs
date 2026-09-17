using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    public class InProcessEmitBuilder : IBuilder
    {
        public bool GetSupportsConcurrency(BuildPartition buildPartition) => true;

        public async ValueTask<BuildResult> BuildAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken)
        {
            await ThreadPoolHelper.Switch(cancellationToken);

            var artifactsPath = GetArtifactsPaths(buildPartition, rootArtifactsFolderPath);
            var assembly = RunnableEmitter.EmitPartitionAssembly(artifactsPath, buildPartition, logger);

            // HACK: use custom artifacts path class to pass the generated assembly.
            return BuildResult.Success(new InProcessEmitArtifactsPath(assembly, artifactsPath));
        }

        private static string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath) => buildArtifactsDirectoryPath;

        private static string GetExecutableExtension() => ".dll";

        private static string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition) => Path.GetDirectoryName(buildPartition.AssemblyLocation)!;

        private static ArtifactsPaths GetArtifactsPaths(BuildPartition buildPartition, string rootArtifactsFolderPath)
        {
            string programName = $"{buildPartition.ProgramName}Emitted";
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
