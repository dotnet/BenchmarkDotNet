using System;
using System.IO;
using System.Xml;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [PublicAPI]
    public class DotNetCliBuilder : IBuilder
    {
        private string TargetFrameworkMoniker { get; }

        private string CustomDotNetCliPath { get; }
        private bool LogOutput { get; }

        [PublicAPI]
        public DotNetCliBuilder(string targetFrameworkMoniker, string? customDotNetCliPath = null, bool logOutput = false)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            CustomDotNetCliPath = customDotNetCliPath;
            LogOutput = logOutput;
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var cliCommand = new DotNetCliCommand(
                generateResult.ArtifactsPaths.BuildForReferencesProjectFilePath,
                CustomDotNetCliPath,
                string.Empty,
                generateResult,
                logger,
                buildPartition,
                Array.Empty<EnvironmentVariable>(),
                buildPartition.Timeout,
                logOutput: LogOutput);

            BuildResult buildResult;
            // Integration tests are built without dependencies, so we skip the first step.
            if (!buildPartition.ForcedNoDependenciesForIntegrationTests)
            {
                // We build the original project first to obtain all dlls.
                buildResult = cliCommand.RestoreThenBuild();

                if (!buildResult.IsBuildSuccess)
                    return buildResult;

                // After the dlls are built, we gather the assembly references, then build the benchmark project.
                GatherReferences(generateResult.ArtifactsPaths);
            }

            buildResult = cliCommand.WithCsProjPath(generateResult.ArtifactsPaths.ProjectFilePath)
                .RestoreThenBuild();

            if (buildResult.IsBuildSuccess &&
                buildPartition.RepresentativeBenchmarkCase.Job.Environment.LargeAddressAware)
            {
                LargeAddressAware.SetLargeAddressAware(generateResult.ArtifactsPaths.ExecutablePath);
            }
            return buildResult;
        }

        internal static void GatherReferences(ArtifactsPaths artifactsPaths)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(artifactsPaths.ProjectFilePath);
            XmlElement projectElement = xmlDoc.DocumentElement;

            // Add reference to every dll.
            var itemGroup = xmlDoc.CreateElement("ItemGroup");
            projectElement.AppendChild(itemGroup);
            foreach (var assemblyFile in Directory.GetFiles(artifactsPaths.BinariesDirectoryPath, "*.dll"))
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyFile);
                // The dummy csproj was used to build the original project, but it also outputs a dll for itself which we need to ignore because it's not valid.
                if (assemblyName == artifactsPaths.ProgramName)
                {
                    continue;
                }
                var referenceElement = xmlDoc.CreateElement("Reference");
                itemGroup.AppendChild(referenceElement);
                referenceElement.SetAttribute("Include", assemblyName);
                var hintPath = xmlDoc.CreateElement("HintPath");
                referenceElement.AppendChild(hintPath);
                var locationNode = xmlDoc.CreateTextNode(assemblyFile);
                hintPath.AppendChild(locationNode);
            }

            xmlDoc.Save(artifactsPaths.ProjectFilePath);
        }
    }
}
