using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.NetCoreApp;
using JetBrains.Annotations;
using System.Reflection;
using System.Xml.Linq;

namespace BenchmarkDotNet.Tests
{
    public class CsProjBuilderTests
    {
        private FileInfo TestAssemblyFileInfo = new FileInfo(typeof(CsProjBuilderTests).Assembly.Location);
        private const string runtimeHostConfigurationOptionChunk = """
            <ItemGroup>
              <RuntimeHostConfigurationOption Include="System.Runtime.Loader.UseRidGraph" Value="true" />
            </ItemGroup>
            """;

        [Theory]
        [InlineData("net471")]
        [InlineData("netcoreapp3.1")]
        public void ItsPossibleToCustomizeProjectSdkBasedOnProjectSdkFromTheProjectFile(string targetFrameworkMoniker)
        {
            const string withCustomProjectSdk = """
                <Project Sdk="CUSTOM">
                </Project>
                """;
            AssertParsedSdkName(withCustomProjectSdk, targetFrameworkMoniker, "CUSTOM");
        }

        [Fact]
        public void ItsImpossibleToCustomizeProjectSdkForFullFrameworkAppsBasedOnTheImportOfSdk()
        {
            const string withCustomProjectImport = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <Import Sdk="Microsoft.NET.Sdk.WindowsDesktop" Project="Sdk.props" Condition="'$(TargetFramework)'=='netcoreapp3.1'"/>
                </Project>
                """;
            AssertParsedSdkName(withCustomProjectImport, "net471", "Microsoft.NET.Sdk");
        }

        [Fact]
        public void ItsPossibleToCustomizeProjectSdkForNetCoreAppsBasedOnTheImportOfSdk()
        {
            const string withCustomProjectImport = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <Import Sdk="Microsoft.NET.Sdk.WindowsDesktop" Project="Sdk.props" Condition="'$(TargetFramework)'=='netcoreapp3.1'"/>
                </Project>
                """;
            AssertParsedSdkName(withCustomProjectImport, "netcoreapp3.1", "Microsoft.NET.Sdk.WindowsDesktop");
        }

        [AssertionMethod]
        private void AssertParsedSdkName(string csProjContent, string targetFrameworkMoniker, string expectedSdkValue)
        {
            var sut = new CsProjBuilder(new NetCoreAppSettings { TargetFrameworkMoniker = targetFrameworkMoniker });

            var benchmarkProject = XElement.Parse(csProjContent);

            Assert.Equal(expectedSdkValue, sut.GetSdkName(benchmarkProject));
            Assert.Empty(CsProjBuilder.GetSettingsToCopy(benchmarkProject, TestAssemblyFileInfo));
        }

        [Fact]
        public void SdkNameAndVersionAreParsedFromTheSdkElement()
        {
            const string withSdkElement = """
                <Project>
                  <Sdk Name="CUSTOM" Version="1.2.3" />
                </Project>
                """;
            AssertParsedSdkName(withSdkElement, "net471", "CUSTOM/1.2.3");
        }

        [AssertionMethod]
        private void AssertCopiedSettings(string csProjContent, string expectedSettings)
        {
            var copied = Assert.Single(CsProjBuilder.GetSettingsToCopy(XElement.Parse(csProjContent), TestAssemblyFileInfo));

            Assert.Equal(XElement.Parse(expectedSettings).ToString(), copied.ToString());
        }

        [Fact]
        public void UseWpfSettingGetsCopied()
        {
            const string withUseWpfTrue = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <PlatformTarget>AnyCPU</PlatformTarget>
                    <UseWpf>true</UseWpf>
                  </PropertyGroup>
                </Project>
                """;
            AssertCopiedSettings(withUseWpfTrue, """
                <PropertyGroup>
                  <UseWpf>true</UseWpf>
                </PropertyGroup>
                """);
        }

        [Fact]
        public void SettingsFromPropsFileImportedUsingAbsolutePathGetCopies()
        {
            const string imported = """
                <Project>
                  <PropertyGroup>
                    <LangVersion>9.9</LangVersion>
                  </PropertyGroup>
                </Project>
                """;
            var propsFilePath = Path.Combine(TestAssemblyFileInfo.DirectoryName!, "test.props");
            File.WriteAllText(propsFilePath, imported);

            string importingAbsolutePath = $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <Import Project="{propsFilePath}" />
                </Project>
                """;

            AssertCopiedSettings(importingAbsolutePath, """
                <PropertyGroup>
                  <LangVersion>9.9</LangVersion>
                </PropertyGroup>
                """);

            File.Delete(propsFilePath);
        }

        [Fact]
        public void SettingsFromPropsFileImportedUsingRelativePathGetCopies()
        {
            const string imported = """
                <Project>
                  <PropertyGroup>
                    <LangVersion>9.9</LangVersion>
                  </PropertyGroup>
                </Project>
                """;
            var propsFilePath = Path.Combine(TestAssemblyFileInfo.DirectoryName!, "test.props");
            File.WriteAllText(propsFilePath, imported);

            string importingRelativePath = $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <Import Project=".{Path.DirectorySeparatorChar}test.props" />
                </Project>
                """;

            AssertCopiedSettings(importingRelativePath, """
                <PropertyGroup>
                  <LangVersion>9.9</LangVersion>
                </PropertyGroup>
                """);

            File.Delete(propsFilePath);
        }

        [Fact]
        public void RuntimeHostConfigurationOptionIsCopied()
        {
            string source = $"""
                <Project Sdk="Microsoft.NET.Sdk">
                {runtimeHostConfigurationOptionChunk}
                </Project>
                """;

            AssertCopiedSettings(source, runtimeHostConfigurationOptionChunk);
        }

        [Fact]
        public void WarningsAsErrorsSettingGetsCopied()
        {
            const string withWarningsAsErrors = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <WarningsAsErrors>NU1102;NU1603</WarningsAsErrors>
                  </PropertyGroup>
                </Project>
                """;
            AssertCopiedSettings(withWarningsAsErrors, """
                <PropertyGroup>
                  <WarningsAsErrors>NU1102;NU1603</WarningsAsErrors>
                </PropertyGroup>
                """);
        }

        [Fact]
        public void TheDefaultFilePathShouldBeUsedWhenAnAssemblyLocationIsEmpty()
        {
            const string programName = "testProgram";
            var config = ManualConfig.CreateEmpty().CreateImmutableConfig();

            //Simulate loading an assembly from a stream
            var benchmarkDotNetAssembly = typeof(MockFactory.MockBenchmarkClass).GetTypeInfo().Assembly;
            var streamLoadedAssembly = Assembly.Load(File.ReadAllBytes(benchmarkDotNetAssembly.Location));
            var assemblyType = streamLoadedAssembly.GetRunnableBenchmarks().Select(type => type).First();

            var target = new Descriptor(assemblyType, MockFactory.MockMethodInfo);
            var benchmarkCase = BenchmarkCase.Create(target, Job.Default, ParameterInstances.Empty, config);

            var benchmarks = new[] { new BenchmarkBuildInfo(benchmarkCase, config.CreateImmutableConfig(), 999, new([])) };
            var projectBuilder = new SteamLoadedBuildPartition(new NetCoreAppSettings { TargetFrameworkMoniker = "netcoreapp3.1" });
            string binariesPath = projectBuilder.ResolvePathForBinaries(new BuildPartition(benchmarks, new Resolver()), programName);

            string expectedPath = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Bin"), programName);
            Assert.Equal(expectedPath, binariesPath);
        }

        [Fact]
        public void TestAssemblyFilePathIsUsedWhenTheAssemblyLocationIsNotEmpty()
        {
            const string programName = "testProgram";
            var target = new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo);
            var benchmarkCase = BenchmarkCase.Create(target, Job.Default, ParameterInstances.Empty, ManualConfig.CreateEmpty().CreateImmutableConfig());
            var benchmarks = new[] { new BenchmarkBuildInfo(benchmarkCase, ManualConfig.CreateEmpty().CreateImmutableConfig(), 0, new([])) };
            var projectBuilder = new SteamLoadedBuildPartition(new NetCoreAppSettings { TargetFrameworkMoniker = "netcoreapp3.1" });
            var buildPartition = new BuildPartition(benchmarks, new Resolver());
            string binariesPath = projectBuilder.ResolvePathForBinaries(buildPartition, programName);

            string expectedPath = Path.Combine(Path.GetDirectoryName(buildPartition.AssemblyLocation)!, programName);
            Assert.Equal(expectedPath, binariesPath);
        }

        [Fact]
        public void FindProjectFileIgnoresDotAndHiddenDirectories()
        {
            var temporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);

            try
            {
                var rootProjectDir = Path.Combine(temporaryDirectory, "XXX.Benchmark");
                Directory.CreateDirectory(rootProjectDir);
                var rootProjectFile = Path.Combine(rootProjectDir, "XXX.Benchmark.csproj");
                File.WriteAllText(rootProjectFile, "<Project />");

                var hiddenProjectDir = Path.Combine(temporaryDirectory, ".claude", "worktrees", "XXX.Benchmark");
                Directory.CreateDirectory(hiddenProjectDir);
                var hiddenProjectFile = Path.Combine(hiddenProjectDir, "XXX.Benchmark.csproj");
                File.WriteAllText(hiddenProjectFile, "<Project />");

                var foundProject = BenchmarkDotNet.Toolchains.CsProj.Helpers.FindProjectFile(new DirectoryInfo(temporaryDirectory), "XXX.Benchmark");

                Assert.Equal(Path.GetFullPath(rootProjectFile), Path.GetFullPath(foundProject.FullName));
            }
            finally
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }

        private class SteamLoadedBuildPartition : CsProjBuilder
        {
            internal string ResolvePathForBinaries(BuildPartition buildPartition, string programName)
            {
                return base.GetBuildArtifactsDirectoryPath(buildPartition, programName);
            }

            public SteamLoadedBuildPartition(NetCoreAppSettings settings)
                : base(settings) { }
        }
    }
}
