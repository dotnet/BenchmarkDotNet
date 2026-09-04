using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.NativeAot;
using System.Xml.Linq;

namespace BenchmarkDotNet.Tests
{
    public class AotProjectGeneratorTests
    {
        private const string ExpectedIntermediateOutputPath = "$([MSBuild]::NormalizeDirectory('$(MSBuildProjectDirectory)', 'o'))";

        [Fact]
        public void NativeAotProjectUsesShortBuildPaths()
        {
            var config = ManualConfig.CreateEmpty().CreateImmutableConfig();
            var benchmark = BenchmarkCase.Create(
                new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                Job.Default,
                ParameterInstances.Empty,
                config);
            var buildPartition = new BuildPartition(
                [new BenchmarkBuildInfo(benchmark, config, 0, new([]))],
                BenchmarkRunnerClean.DefaultResolver);
            var generator = new Generator(
                ilCompilerVersion: "",
                runtimeFrameworkVersion: "",
                targetFrameworkMoniker: "net10.0",
                cliPath: "",
                runtimeIdentifier: "win-x64",
                feeds: new Dictionary<string, string>(),
                useNuGetClearTag: false,
                useTempFolderForRestore: false,
                packagesRestorePath: "",
                rootAllApplicationAssemblies: false,
                ilcGenerateStackTraceData: true,
                ilcOptimizationPreference: "Speed",
                ilcInstructionSet: "");

            string project = generator.GenerateProjectForNuGetBuild(
                "Benchmarks.csproj",
                buildPartition,
                ArtifactsPaths.Empty,
                NullLogger.Instance);

            AssertShortIntermediatePath(XDocument.Parse(project));
        }

        [Fact]
        public void ReadyToRunProjectUsesShortBuildPaths()
        {
            AssertShortIntermediatePath(XDocument.Parse(ResourceHelper.LoadTemplate("R2RCsProj.txt")));
        }

        private static void AssertShortIntermediatePath(XDocument project)
        {
            Assert.Equal(ExpectedIntermediateOutputPath, project.Descendants("IntermediateOutputPath").Single().Value);
            Assert.Empty(project.Descendants("BaseIntermediateOutputPath"));
        }
    }
}
