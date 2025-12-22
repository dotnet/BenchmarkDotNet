using System.IO;
using System.Text;
using System.Xml;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CompositeR2R
{
    public class CompositeR2RGenerator : CsProjGenerator
    {
        private readonly string CustomRuntimePack;
        private readonly string Crossgen2Pack;

        public CompositeR2RGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string customRuntimePack, string crossgen2Pack)
            : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion: null)
        {
            CustomRuntimePack = customRuntimePack;
            Crossgen2Pack = crossgen2Pack;
        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectFile.FullName);
            var (customProperties, sdkName) = GetSettingsThatNeedToBeCopied(xmlDoc, projectFile);

            string content = new StringBuilder(ResourceHelper.LoadTemplate("CompositeR2RCsProj.txt"))
                .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                .Replace("$CSPROJPATH$", projectFile.FullName)
                .Replace("$TFM$", TargetFrameworkMoniker)
                .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                .Replace("$COPIEDSETTINGS$", customProperties)
                .Replace("$SDKNAME$", sdkName)
                .Replace("$RUNTIMEPACK$", CustomRuntimePack)
                .Replace("$CROSSGEN2PACK$", Crossgen2Pack)
                .Replace("$RUNTIMEIDENTIFIER$", CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier())
                .ToString();

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);

            GatherReferences(projectFile.FullName, buildPartition, artifactsPaths, logger);
        }

        protected override string GetExecutablePath(string binariesDirectoryPath, string programName)
            => OsDetector.IsWindows()
                ? Path.Combine(binariesDirectoryPath, $"{programName}.exe")
                : Path.Combine(binariesDirectoryPath, programName);

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier(), "publish");
    }
}
