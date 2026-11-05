using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.NetCoreApp;
using BenchmarkDotNet.Toolchains.R2R;
using JetBrains.Annotations;
using System.Reflection;
using System.Xml.Linq;

namespace BenchmarkDotNet.Tests
{
    public class CsProjBuilderTests(ITestOutputHelper output)
    {
        private const string GatheringCondition = "'$(BenchmarkDotNetGatherReferences)' == 'true'";
        private const string NotGatheringCondition = "'$(BenchmarkDotNetGatherReferences)' != 'true'";
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
        public void BuildArtifactsGoBesideTheBenchmarkProjectWhenTheAssemblyLocationIsEmpty()
        {
            const string programName = "testProgram";
            var config = ManualConfig.CreateEmpty().CreateImmutableConfig();

            //Simulate loading an assembly from a stream
            var benchmarkDotNetAssembly = typeof(MockFactory.MockBenchmarkClass).GetTypeInfo().Assembly;
            var streamLoadedAssembly = Assembly.Load(File.ReadAllBytes(benchmarkDotNetAssembly.Location));
            var assemblyType = streamLoadedAssembly.GetRunnableBenchmarks().Select(type => type).First();
            var assemblyMethod = assemblyType.GetMethods().First(method => method.HasAttribute<global::BenchmarkDotNet.Attributes.BenchmarkAttribute>());

            var target = new Descriptor(assemblyType, assemblyMethod);
            var benchmarkCase = BenchmarkCase.Create(target, Job.Default, ParameterInstances.Empty, config);

            var benchmarks = new[] { new BenchmarkBuildInfo(benchmarkCase, config.CreateImmutableConfig(), 999, new([])) };
            var projectBuilder = new SteamLoadedBuildPartition(new NetCoreAppSettings { TargetFrameworkMoniker = "netcoreapp3.1" });
            string binariesPath = projectBuilder.ResolvePathForBinaries(new BuildPartition(benchmarks, new Resolver()), programName);

            var bdnDirectory = new DirectoryInfo(Path.GetDirectoryName(binariesPath)!);
            Assert.Equal(CsProjBuilder.ProjectLocalFolderName, bdnDirectory.Name);
            Assert.True(File.Exists(Path.Combine(bdnDirectory.Parent!.FullName, "BenchmarkDotNet.Tests.csproj")));
        }

        [Fact]
        public void BuildArtifactsGoInAHiddenFolderBesideTheBenchmarkProject()
        {
            const string programName = "testProgram";
            var target = new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo);
            var benchmarkCase = BenchmarkCase.Create(target, Job.Default, ParameterInstances.Empty, ManualConfig.CreateEmpty().CreateImmutableConfig());
            var benchmarks = new[] { new BenchmarkBuildInfo(benchmarkCase, ManualConfig.CreateEmpty().CreateImmutableConfig(), 0, new([])) };
            var projectBuilder = new SteamLoadedBuildPartition(new NetCoreAppSettings { TargetFrameworkMoniker = "netcoreapp3.1" });
            var buildPartition = new BuildPartition(benchmarks, new Resolver());
            string binariesPath = projectBuilder.ResolvePathForBinaries(buildPartition, programName);

            var bdnDirectory = new DirectoryInfo(Path.GetDirectoryName(binariesPath)!);
            Assert.Equal(CsProjBuilder.ProjectLocalFolderName, bdnDirectory.Name);
            Assert.True(File.Exists(Path.Combine(bdnDirectory.Parent!.FullName, "BenchmarkDotNet.Tests.csproj")));
            Assert.Equal(CsProjBuilder.ToBase36(buildPartition.Id), Path.GetFileName(binariesPath));
        }

        [Theory]
        [InlineData(0, "0")]
        [InlineData(9, "9")]
        [InlineData(10, "a")]
        [InlineData(35, "z")]
        [InlineData(36, "10")]
        [InlineData(1295, "zz")]
        [InlineData(int.MaxValue, "zik0zj")]
        public void PartitionIdsAreEncodedAsLowercaseBase36(int id, string expected)
            => Assert.Equal(expected, CsProjBuilder.ToBase36(id));

        [Theory]
        [InlineData(CsProjBuilder.ProjectLocalFolderName)]
        [InlineData("elsewhere")]
        public void GeneratingHidesOnlyTheProjectLocalFolder(string parentName)
        {
            var temporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                var buildArtifactsDirectory = Path.Combine(temporaryDirectory, parentName, "testProgram");
                var binariesDirectory = Path.Combine(buildArtifactsDirectory, "b");
                var artifactsPaths = new ArtifactsPaths("", buildArtifactsDirectory, binariesDirectory, "", "", "", "", "", "", "", "testProgram", "");

                new SteamLoadedBuildPartition(new NetCoreAppSettings { TargetFrameworkMoniker = "netcoreapp3.1" }).CopyRequiredFiles(artifactsPaths);

                var parentAttributes = new DirectoryInfo(Path.Combine(temporaryDirectory, parentName)).Attributes;
                Assert.Equal(parentName == CsProjBuilder.ProjectLocalFolderName, parentAttributes.HasFlag(FileAttributes.Hidden));
                Assert.True(Directory.Exists(binariesDirectory));
            }
            finally
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }

        [Fact]
        public void FreshPackagesFolderIsRestoredInsideTheBuildDirectory()
        {
            var buildArtifactsDirectory = Path.Combine(Path.GetTempPath(), ".bdn", "testProgram");
            var explicitPackages = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "packages"));

            string Resolve(NetCoreAppSettings settings) => new SteamLoadedBuildPartition(settings).ResolvePackagesPath(buildArtifactsDirectory);

            Assert.Equal("", Resolve(new NetCoreAppSettings()));
            Assert.Equal(Path.Combine(buildArtifactsDirectory, CsProjBuilder.RestoreFolderName), Resolve(new NetCoreAppSettings { UseFreshPackages = true }));
            Assert.Equal(explicitPackages.FullName, Resolve(new NetCoreAppSettings { UseFreshPackages = true, PackagesPath = explicitPackages }));
        }

        [Theory]
        [InlineData("net10.0", "CoreCLR")]
        [InlineData("net11.0", "CoreCLR")]
        [InlineData("net12.0", "CoreCLR")]
        [InlineData("net10.0", "R2R")]
        [InlineData("net11.0", "R2R")]
        [InlineData("net12.0", "R2R")]
        [InlineData("net10.0", "Mono")]
        [InlineData("net11.0", "Mono")]
        [InlineData("net12.0", "Mono")]
        [InlineData("net10.0", "NativeAOT")]
        [InlineData("net11.0", "NativeAOT")]
        [InlineData("net12.0", "NativeAOT")]
        public void ReferenceGatheringDoesNotChangeTheFinalDeploymentMode(string targetFrameworkMoniker, string toolchain)
        {
            var runtimePack = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "runtime & 'pack'"));
            var crossgen2Pack = new FileInfo(Path.Combine(Path.GetTempPath(), "crossgen2 & 'pack'"));
            CsProjBuilder builder = toolchain switch
            {
                "CoreCLR" => new CsProjBuilder(new NetCoreAppSettings { TargetFrameworkMoniker = targetFrameworkMoniker }),
                "R2R" => new R2RBuilder(new R2RSettings
                {
                    TargetFrameworkMoniker = targetFrameworkMoniker,
                    CustomRuntimePack = runtimePack,
                    Crossgen2Pack = crossgen2Pack
                }),
                "Mono" => new CsProjMonoBuilder(new MonoCoreSettings { TargetFrameworkMoniker = targetFrameworkMoniker }),
                "NativeAOT" => new CsProjNativeAotBuilder(new NativeAotSettings { TargetFrameworkMoniker = targetFrameworkMoniker }),
                _ => throw new ArgumentOutOfRangeException(nameof(toolchain))
            };
            var config = ManualConfig.CreateEmpty().CreateImmutableConfig();
            var benchmarkCase = BenchmarkCase.Create(new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
                Job.Default, ParameterInstances.Empty, config);
            var partition = new BuildPartition([new BenchmarkBuildInfo(benchmarkCase, config, 0, new([]))], BenchmarkRunnerClean.DefaultResolver);
            var project = XDocument.Parse(builder.CreateProject(partition, ArtifactsPaths.Empty, NullLogger.Instance).ToString()).Root!;
            output.WriteLine(project.ToString());

            var finalProperties = GetDeploymentProperties(project, gathering: false);
            if (toolchain is "R2R" or "Mono")
            {
                Assert.Equal("true", finalProperties["SelfContained"]);
                Assert.Equal(RuntimeInformation.GetPortableRuntimeIdentifier(), finalProperties["RuntimeIdentifier"]);
            }
            else
                Assert.False(finalProperties.ContainsKey("SelfContained"));

            foreach (string property in new[] { "UseAppHost", "SkipCompilerExecution", "SkipCopyBuildProduct" })
                Assert.False(finalProperties.ContainsKey(property));

            if (toolchain == "R2R")
            {
                Assert.Equal("true", finalProperties["PublishReadyToRun"]);
                Assert.Equal("true", finalProperties["PublishReadyToRunComposite"]);
                var target = Assert.Single(project.Elements("Target"), target => (string?)target.Attribute("Name") == "TrickRuntimePackLocation");
                Assert.Equal("ProcessFrameworkReferences", (string?)target.Attribute("AfterTargets"));
                Assert.Null(target.Attribute("Condition"));
                Assert.Equal(runtimePack.FullName, target.Element("ItemGroup")!.Element("RuntimePack")!.Element("PackageDirectory")!.Value);
                Assert.Equal(crossgen2Pack.FullName, target.Element("ItemGroup")!.Element("Crossgen2Pack")!.Element("PackageDirectory")!.Value);
            }
            else if (toolchain == "Mono")
                Assert.Equal("true", finalProperties["UseMonoRuntime"]);
            else if (toolchain == "NativeAOT")
                Assert.Equal("true", finalProperties["PublishAot"]);

            var referenceTarget = Assert.Single(project.Elements("Target"), target => (string?)target.Attribute("Name") == "BdnWriteReferences");
            Assert.Equal(GatheringCondition, (string?)referenceTarget.Attribute("Condition"));
            var referenceImport = Assert.Single(project.Elements("Import"));
            Assert.Equal("$(MSBuildThisFileDirectory)ref.props", (string?)referenceImport.Attribute("Project"));
            Assert.Equal($"{NotGatheringCondition} And Exists('$(MSBuildThisFileDirectory)ref.props')", (string?)referenceImport.Attribute("Condition"));

            var gatherProperties = GetDeploymentProperties(project, gathering: true);
            Assert.Equal("false", gatherProperties["UseAppHost"]);
            Assert.Equal("true", gatherProperties["SkipCompilerExecution"]);
            Assert.Equal("true", gatherProperties["SkipCopyBuildProduct"]);
            Assert.True(gatherProperties.TryGetValue("SelfContained", out string? selfContained));
            Assert.Equal("false", selfContained);
            Assert.Equal("false", Assert.Single(project.Elements("PropertyGroup")
                .Where(group => (string?)group.Attribute("Condition") == GatheringCondition)
                .Elements("SelfContained")).Value);
        }

        // Evaluate the generated deployment declarations in document order, not SDK defaults or imported targets.
        private static Dictionary<string, string> GetDeploymentProperties(XElement project, bool gathering)
        {
            var properties = new Dictionary<string, string>();
            foreach (var group in project.Elements("PropertyGroup"))
            {
                string? condition = (string?)group.Attribute("Condition");
                if (condition is not null && condition != (gathering ? GatheringCondition : NotGatheringCondition))
                    continue;

                foreach (var property in group.Elements().Where(property => property.Attribute("Condition") is null))
                    properties[property.Name.LocalName] = property.Value;
            }
            return properties;
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

            internal void CopyRequiredFiles(ArtifactsPaths artifactsPaths) => CopyAllRequiredFiles(artifactsPaths);

            internal string ResolvePackagesPath(string buildArtifactsDirectoryPath) => GetPackagesDirectoryPath(buildArtifactsDirectoryPath);

            public SteamLoadedBuildPartition(NetCoreAppSettings settings)
                : base(settings) { }
        }
    }
}
