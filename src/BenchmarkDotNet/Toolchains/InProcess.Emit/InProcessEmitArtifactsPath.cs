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
            baseArtifacts.ProgramCodePath,
            baseArtifacts.AppConfigPath,
            baseArtifacts.NuGetConfigPath,
            baseArtifacts.ProjectFilePath,
            baseArtifacts.BuildScriptFilePath,
            baseArtifacts.ExecutablePath,
            baseArtifacts.ProgramName,
            baseArtifacts.PackagesDirectoryName)
        {
            GeneratedAssembly = generatedAssembly;
        }
    }
}