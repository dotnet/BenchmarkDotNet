using System;

namespace BenchmarkDotNet.Toolchains
{
    public class ArtifactsPaths
    {
        public Action<ArtifactsPaths> Cleanup { get; }

        public string RootArtifactsFolderPath { get; }

        public string BuildArtifactsDirectoryPath { get; }

        public string BinariesDirectoryPath { get; }

        public string ProgramCodePath { get; }

        public string AppConfigPath { get; }

        public string ProjectFilePath { get; }

        public string BuildScriptFilePath { get; }

        public string ExecutablePath { get; }

        public string CompilerPath { get; }

        public ArtifactsPaths(
            Action<ArtifactsPaths> cleanup,
            string rootArtifactsFolderPath,
            string buildArtifactsDirectoryPath,
            string binariesDirectoryPath,
            string programCodePath,
            string appConfigPath,
            string projectFilePath,
            string buildScriptFilePath,
            string executablePath,
            string compilerPath = null)
        {
            Cleanup = cleanup;
            RootArtifactsFolderPath = rootArtifactsFolderPath;
            BuildArtifactsDirectoryPath = buildArtifactsDirectoryPath;
            BinariesDirectoryPath = binariesDirectoryPath;
            ProgramCodePath = programCodePath;
            AppConfigPath = appConfigPath;
            ProjectFilePath = projectFilePath;
            BuildScriptFilePath = buildScriptFilePath;
            ExecutablePath = executablePath;
            CompilerPath = compilerPath;
        }

        public void RemoveBenchmarkFiles()
        {
            Cleanup.Invoke(this);
        }
    }
}