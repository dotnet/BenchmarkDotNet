using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;
using BenchmarkDotNet.Toolchains.Results;
using System;
using System.Collections.Generic;
using System.IO;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    public class InProcessEmitGenerator : IGenerator
    {
        public GenerateResult GenerateProject(
            BuildPartition buildPartition,
            ILogger logger,
            string rootArtifactsFolderPath)
        {
            var artifactsPaths = ArtifactsPaths.Empty;
            try
            {
                artifactsPaths = GetArtifactsPaths(buildPartition, rootArtifactsFolderPath);

                return GenerateResult.Success(artifactsPaths, new List<string>());
            }
            catch (Exception ex)
            {
                logger.WriteLineError($"Failed to generate partition: {ex}");
                return GenerateResult.Failure(artifactsPaths, new List<string>());
            }
        }

        private string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath) => buildArtifactsDirectoryPath;

        private string GetExecutableExtension() => ".dll";

        private string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition) => Path.GetDirectoryName(buildPartition.AssemblyLocation);

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
                programCodePath: null,
                appConfigPath: null,
                nuGetConfigPath: null,
                projectFilePath: null,
                buildScriptFilePath: null,
                executablePath: executablePath,
                programName: programName,
                packagesDirectoryName: null);
        }
    }
}