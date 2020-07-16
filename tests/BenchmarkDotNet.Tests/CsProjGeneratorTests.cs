﻿using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains.CsProj;
using JetBrains.Annotations;
using Xunit;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Tests
{
    public class CsProjGeneratorTests
    {
        private FileInfo TestAssemblyFileInfo = new FileInfo(typeof(CsProjGeneratorTests).Assembly.Location);

        [Theory]
        [InlineData("net471", false)]
        [InlineData("netcoreapp3.0", true)]
        public void ItsPossibleToCustomizeProjectSdkBasedOnProjectSdkFromTheProjectFile(string targetFrameworkMoniker, bool isNetCore)
        {
            const string withCustomProjectSdk = @"
<Project Sdk=""CUSTOM"">
</Project>
";
            AssertParsedSdkName(withCustomProjectSdk, targetFrameworkMoniker, "CUSTOM", isNetCore);
        }

        [Fact]
        public void ItsImpossibleToCustomizeProjectSdkForFullFrameworkAppsBasedOnTheImportOfSdk()
        {
            const string withCustomProjectImport = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <Import Sdk=""Microsoft.NET.Sdk.WindowsDesktop"" Project=""Sdk.props"" Condition=""'$(TargetFramework)'=='netcoreapp3.0'""/>
</Project>
";
            AssertParsedSdkName(withCustomProjectImport, "net471", "Microsoft.NET.Sdk", false);
        }

        [Fact]
        public void ItsPossibleToCustomizeProjectSdkForNetCoreAppsBasedOnTheImportOfSdk()
        {
            const string withCustomProjectImport = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <Import Sdk=""Microsoft.NET.Sdk.WindowsDesktop"" Project=""Sdk.props"" Condition=""'$(TargetFramework)'=='netcoreapp3.0'""/>
</Project>
";
            AssertParsedSdkName(withCustomProjectImport, "netcoreapp3.0", "Microsoft.NET.Sdk.WindowsDesktop", true);
        }

        [AssertionMethod]
        private void AssertParsedSdkName(string csProjContent, string targetFrameworkMoniker, string expectedSdkValue, bool isNetCore)
        {
            var sut = new CsProjGenerator(targetFrameworkMoniker, null, null, null, isNetCore);

            using (var reader = new StringReader(csProjContent))
            {
                var (customProperties, sdkName) = sut.GetSettingsThatNeedsToBeCopied(reader, TestAssemblyFileInfo);

                Assert.Equal(expectedSdkValue, sdkName);
                Assert.Empty(customProperties);
            }
        }

        [Fact]
        public void UseWpfSettingGetsCopied()
        {
            const string withUseWpfTrue = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <UseWpf>true</UseWpf>
  </PropertyGroup>
</Project>
";
            var sut = new CsProjGenerator("netcoreapp3.0", null, null, null, true);

            using (var reader = new StringReader(withUseWpfTrue))
            {
                var (customProperties, sdkName) = sut.GetSettingsThatNeedsToBeCopied(reader, TestAssemblyFileInfo);

                Assert.Equal("<UseWpf>true</UseWpf>", customProperties);
                Assert.Equal("Microsoft.NET.Sdk", sdkName);
            }
        }

        [Fact]
        public void SettingsFromPropsFileImportedUsingAbsolutePathGetCopies()
        {
            const string imported = @"
<Project>
  <PropertyGroup>
    <LangVersion>9.9</LangVersion>
  </PropertyGroup>
</Project>
";
            var propsFilePath = Path.Combine(TestAssemblyFileInfo.DirectoryName, "test.props");
            File.WriteAllText(propsFilePath, imported);

            string importingAbsolutePath = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <Import Project=""{propsFilePath}"" />
</Project>";

            var sut = new CsProjGenerator("netcoreapp3.0", null, null, null, true);

            using (var reader = new StringReader(importingAbsolutePath))
            {
                var (customProperties, sdkName) = sut.GetSettingsThatNeedsToBeCopied(reader, TestAssemblyFileInfo);

                Assert.Equal("<LangVersion>9.9</LangVersion>", customProperties);
                Assert.Equal("Microsoft.NET.Sdk", sdkName);
            }

            File.Delete(propsFilePath);
        }

        [Fact]
        public void SettingsFromPropsFileImportedUsingRelativePathGetCopies()
        {
            const string imported = @"
<Project>
  <PropertyGroup>
    <LangVersion>9.9</LangVersion>
  </PropertyGroup>
</Project>
";
            var propsFilePath = Path.Combine(TestAssemblyFileInfo.DirectoryName, "test.props");
            File.WriteAllText(propsFilePath, imported);

            string importingRelativePath = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <Import Project="".{Path.DirectorySeparatorChar}test.props"" />
</Project>";

            var sut = new CsProjGenerator("netcoreapp3.0", null, null, null, true);

            using (var reader = new StringReader(importingRelativePath))
            {
                var (customProperties, sdkName) = sut.GetSettingsThatNeedsToBeCopied(reader, TestAssemblyFileInfo);

                Assert.Equal("<LangVersion>9.9</LangVersion>", customProperties);
                Assert.Equal("Microsoft.NET.Sdk", sdkName);
            }

            File.Delete(propsFilePath);
        }

        [Fact]
        public void TheDefaultFilePathShouldBeUsedWhenAnAssemblyLocationIsEmpty()
        {
            const string programName = "testProgram";
            var config = ManualConfig.CreateEmpty().CreateImmutableConfig();
            var benchmarkMethod =
                typeof(MockFactory.MockBenchmarkClass)
                    .GetTypeInfo()
                    .GetMethods()
                    .Single(method => method.Name == nameof(MockFactory.MockBenchmarkClass.Foo));


            //Simulate loading an assembly from a stream
            var benchmarkDotNetAssembly = typeof(MockFactory.MockBenchmarkClass).GetTypeInfo().Assembly;
            var streamLoadedAssembly = Assembly.Load(File.ReadAllBytes(benchmarkDotNetAssembly.Location));
            var assemblyType = streamLoadedAssembly.GetRunnableBenchmarks().Select(type => type).FirstOrDefault();

            var target = new Descriptor(assemblyType, benchmarkMethod);
            var benchmarkCase = BenchmarkCase.Create(target, Job.Default, null, config);

            var benchmarks = new[] { new BenchmarkBuildInfo(benchmarkCase, config.CreateImmutableConfig(), 999) };
            var projectGenerator = new SteamLoadedBuildPartition("netcoreapp3.0", null, null, null, true);
            string binariesPath = projectGenerator.ResolvePathForBinaries(new BuildPartition(benchmarks, new Resolver()), programName);

            string expectedPath = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Bin"), programName);
            Assert.Equal(expectedPath, binariesPath);
        }

        [Fact]
        public void TestAssemblyFilePathIsUsedWhenTheAssemblyLocationIsNotEmpty()
        {
            const string programName = "testProgram";
            var benchmarkMethod =
                typeof(MockFactory.MockBenchmarkClass)
                    .GetTypeInfo()
                    .GetMethods()
                    .Single(method => method.Name == nameof(MockFactory.MockBenchmarkClass.Foo));
            var target = new Descriptor(typeof(MockFactory.MockBenchmarkClass), benchmarkMethod);
            var benchmarkCase = BenchmarkCase.Create(target, Job.Default, null, ManualConfig.CreateEmpty().CreateImmutableConfig());
            var benchmarks = new[] { new BenchmarkBuildInfo(benchmarkCase, ManualConfig.CreateEmpty().CreateImmutableConfig(), 0) };
            var projectGenerator = new SteamLoadedBuildPartition("netcoreapp3.0", null, null, null, true);
            var buildPartition = new BuildPartition(benchmarks, new Resolver());
            string binariesPath = projectGenerator.ResolvePathForBinaries(buildPartition, programName);

            string expectedPath = Path.Combine(Path.GetDirectoryName(buildPartition.AssemblyLocation), programName);
            Assert.Equal(expectedPath, binariesPath);
        }

        private class SteamLoadedBuildPartition : CsProjGenerator
        {
            internal string ResolvePathForBinaries(BuildPartition buildPartition, string programName)
            {
                return base.GetBuildArtifactsDirectoryPath(buildPartition, programName);
            }

            public SteamLoadedBuildPartition(string targetFrameworkMoniker, string cliPath, string packagesPath, string runtimeFrameworkVersion, bool isNetCore)
                : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion, isNetCore) { }
        }
    }
}
