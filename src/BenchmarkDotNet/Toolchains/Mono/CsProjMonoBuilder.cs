using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using System.Xml.Linq;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Toolchains.Mono
{
    internal sealed class CsProjMonoBuilder(MonoCoreSettings settings) : CsProjBuilder(settings)
    {
        protected override bool PublishesOutput => true;

        protected override void AddProjectContent(XElement project, BuildPartition buildPartition, ArtifactsPaths artifactsPaths, FileInfo projectFile)
        {
            base.AddProjectContent(project, buildPartition, artifactsPaths, projectFile);

            // Declared here rather than passed to the cli, where they would be global properties and would
            // reach the referenced projects: a runtime identifier is not something a library reference can
            // resolve against.
            var runtimeIdentifier = RuntimeInformation.GetPortableRuntimeIdentifier();
            project.Add(new XElement("PropertyGroup",
                new XElement("SelfContained", "true"),
                new XElement("RuntimeIdentifier", runtimeIdentifier),
                // RuntimeIdentifiers is set as well because SelfContained requires it.
                // https://github.com/dotnet/sdk/issues/10566
                new XElement("RuntimeIdentifiers", runtimeIdentifier),
                // As a global property this causes NU1102 for projects targeting .NET 9.0 or higher. #3000
                new XElement("UseMonoRuntime", "true")));
        }
    }
}
