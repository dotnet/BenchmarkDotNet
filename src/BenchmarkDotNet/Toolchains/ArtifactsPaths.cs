using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    public class ArtifactsPaths
    {
        public static readonly ArtifactsPaths Empty = new("", "", "", "", "", "", "", "", "", "", "", "");

        [PublicAPI] public string RootArtifactsFolderPath { get; }
        [PublicAPI] public string BuildArtifactsDirectoryPath { get; }
        [PublicAPI] public string BinariesDirectoryPath { get; }
        [PublicAPI] public string PublishDirectoryPath { get; }
        [PublicAPI] public string ProgramCodePath { get; }
        [PublicAPI] public string AppConfigPath { get; }
        [PublicAPI] public string NuGetConfigPath { get; }
        [PublicAPI] public string ProjectFilePath { get; }
        [PublicAPI] public string BuildScriptFilePath { get; }
        [PublicAPI] public string ExecutablePath { get; }
        [PublicAPI] public string ProgramName { get; }
        [PublicAPI] public string PackagesDirectoryName { get; }

        public ArtifactsPaths(
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
            RootArtifactsFolderPath = rootArtifactsFolderPath;
            BuildArtifactsDirectoryPath = buildArtifactsDirectoryPath;
            BinariesDirectoryPath = binariesDirectoryPath;
            PublishDirectoryPath = publishDirectoryPath.EnsureNotNull();
            ProgramCodePath = programCodePath.EnsureNotNull();
            AppConfigPath = appConfigPath.EnsureNotNull();
            NuGetConfigPath = nuGetConfigPath.EnsureNotNull();
            ProjectFilePath = projectFilePath.EnsureNotNull();
            BuildScriptFilePath = buildScriptFilePath.EnsureNotNull();
            ExecutablePath = executablePath;
            ProgramName = programName;
            PackagesDirectoryName = packagesDirectoryName.EnsureNotNull();
        }
    }
}