using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Toolchains
{
    public class ArtifactsPaths
    {
        public static readonly ArtifactsPaths Empty = new ArtifactsPaths("", "", "", "", Array.Empty<string>(), "", "", "", "", "", "", "");

        [PublicAPI] public string RootArtifactsFolderPath { get; }
        [PublicAPI] public string BuildArtifactsDirectoryPath { get; }
        [PublicAPI] public string BinariesDirectoryPath { get; }
        [PublicAPI] public string ProgramCodePath { get; }
        [PublicAPI] public string[] DerivedTypesSourcePaths { get; }
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
            string programCodePath,
            string[] derivedTypesSourcePaths,
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
            ProgramCodePath = programCodePath;
            DerivedTypesSourcePaths = derivedTypesSourcePaths;
            AppConfigPath = appConfigPath;
            NuGetConfigPath = nuGetConfigPath;
            ProjectFilePath = projectFilePath;
            BuildScriptFilePath = buildScriptFilePath;
            ExecutablePath = executablePath;
            ProgramName = programName;
            PackagesDirectoryName = packagesDirectoryName;
        }
    }
}