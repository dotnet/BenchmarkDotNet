using System.Reflection;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    public class InProcessEmitArtifactsPath : ArtifactsPaths
    {
        public Assembly GeneratedAssembly { get; }

        public InProcessEmitArtifactsPath(
            Assembly generatedAssembly,
            ArtifactsPaths baseArtifacts) : base(
            baseArtifacts.RootArtifactsFolderPath,
            baseArtifacts.BuildArtifactsDirectoryPath,
            baseArtifacts.BinariesDirectoryPath,
            baseArtifacts.IntermediateDirectoryPath,
            baseArtifacts.ProgramCodePath,
            baseArtifacts.AppConfigPath,
            baseArtifacts.NuGetConfigPath,
            baseArtifacts.ProjectFilePath,
            baseArtifacts.BuildForReferencesProjectFilePath,
            baseArtifacts.BuildScriptFilePath,
            baseArtifacts.ExecutablePath,
            baseArtifacts.ProgramName,
            baseArtifacts.PackagesDirectoryName)
        {
            GeneratedAssembly = generatedAssembly;
        }
    }
}