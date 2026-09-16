using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using System.Xml.Linq;

namespace BenchmarkDotNet.Toolchains.R2R
{
    internal sealed class R2RGenerator : CsProjGenerator
    {
        private readonly R2RSettings settings;

        public R2RGenerator(R2RSettings settings) : base(settings)
        {
            this.settings = settings;
            BenchmarkRunCallType = Code.CodeGenBenchmarkRunCallType.Direct;
        }

        protected override void AddProjectContent(XElement project, BuildPartition buildPartition, ArtifactsPaths artifactsPaths, FileInfo projectFile)
        {
            base.AddProjectContent(project, buildPartition, artifactsPaths, projectFile);

            project.Add(new XElement("PropertyGroup",
                new XComment(" Shorten obj path to work around https://github.com/dotnet/runtime/issues/103625. "),
                new XElement("IntermediateOutputPath", "$([MSBuild]::NormalizeDirectory('$(MSBuildProjectDirectory)', 'o'))"),
                new XElement("SelfContained", "true"),
                new XElement("RuntimeIdentifier", RuntimeInformation.GetPortableRuntimeIdentifier()),
                new XElement("PublishReadyToRun", "true"),
                new XElement("PublishReadyToRunComposite", "true")));

            project.Add(new XElement("ItemGroup",
                new XComment(" Temporary fix until https://github.com/dotnet/sdk/pull/52296 is resolved "),
                new XElement("PublishReadyToRunCompositeExclusions", new XAttribute("Include", "Dia2Lib.dll")),
                new XElement("PublishReadyToRunCompositeExclusions", new XAttribute("Include", "TraceReloggerLib.dll"))));
        }

        protected override void AddLateProperties(XElement project, BuildPartition buildPartition, ArtifactsPaths artifactsPaths, FileInfo projectFile)
        {
            base.AddLateProperties(project, buildPartition, artifactsPaths, projectFile);

            // The point of this project is to publish the app with r2r composite using a custom runtime pack
            // and a custom crossgen2, both built locally.
            project.Add(new XElement("Target",
                new XAttribute("Name", "TrickRuntimePackLocation"),
                new XAttribute("AfterTargets", "ProcessFrameworkReferences"),
                new XElement("ItemGroup",
                    new XElement("RuntimePack",
                        new XElement("PackageDirectory", settings.CustomRuntimePack?.FullName)),
                    new XElement("Crossgen2Pack",
                        new XElement("PackageDirectory", settings.Crossgen2Pack?.FullName)))));
        }

        protected override bool PublishesOutput => true;

        protected override string GetExecutableExtension() => OsDetector.ExecutableExtension;

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, Settings.TargetFrameworkMoniker, RuntimeInformation.GetPortableRuntimeIdentifier(), "publish");
    }
}
