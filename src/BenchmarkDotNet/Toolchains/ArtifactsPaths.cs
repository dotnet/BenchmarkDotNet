using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Toolchains
{
    public class ArtifactsPaths(
        string rootArtifactsFolderPath,
        string buildArtifactsDirectoryPath,
        string binariesDirectoryPath,
        string publishDirectoryPath,
        string programCodePath,
        string appConfigPath,
        string nuGetConfigPath,
        string projectFilePath,
        string buildScriptFilePath,
        string executablePath,
        string programName,
        string packagesDirectoryName)
    {
        public static readonly ArtifactsPaths Empty = new("", "", "", "", "", "", "", "", "", "", "", "");

        public string RootArtifactsFolderPath { get; } = rootArtifactsFolderPath;
        public string BuildArtifactsDirectoryPath { get; } = buildArtifactsDirectoryPath;
        public string BinariesDirectoryPath { get; } = binariesDirectoryPath;
        public string PublishDirectoryPath { get; } = publishDirectoryPath.EnsureNotNull();
        public string ProgramCodePath { get; } = programCodePath.EnsureNotNull();
        public string AppConfigPath { get; } = appConfigPath.EnsureNotNull();
        public string NuGetConfigPath { get; } = nuGetConfigPath.EnsureNotNull();
        public string ProjectFilePath { get; } = projectFilePath.EnsureNotNull();
        public string BuildScriptFilePath { get; } = buildScriptFilePath.EnsureNotNull();
        public string ExecutablePath { get; } = executablePath;
        public string ProgramName { get; } = programName;
        public string PackagesDirectoryName { get; } = packagesDirectoryName.EnsureNotNull();
    }
}
