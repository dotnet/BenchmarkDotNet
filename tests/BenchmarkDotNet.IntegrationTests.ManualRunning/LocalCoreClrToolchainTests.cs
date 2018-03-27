using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CustomCoreClr;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable InconsistentNaming we use crazy names for the types to include the  version numbers

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    /// <summary>
    /// to run these tests please clone and build CoreClr and CoreFx first,
    /// then update the hardcoded paths and versions
    /// and run following command from console:
    /// dotnet test -c Release -f netcoreapp2.1 --filter "FullyQualifiedName~BenchmarkDotNet.IntegrationTests.ManualRunning.LocalCoreClrToolchainTests"
    /// 
    /// in perfect world we would do this OOB for you, but building CoreCLR and CoreFX takes a LOT of time
    /// so it's not part of our CI jobs
    /// </summary>
    public class LocalCoreClrToolchainTests : BenchmarkTestExecutor
    {
        private const string CoreFxBinPackagesPath = @"C:\Projects\corefx\bin\packages\Debug"; // "/git/corefx/bin/packages/Release/";
        private const string CoreClrBinPackagesPath = @"C:\Projects\coreclr\bin\Product\Windows_NT.x64.Debug\.nuget\pkg"; // "/git/coreclr/bin/Product/Linux.x64.Release/.nuget/pkg/";
        private const string CoreClrPackagesPath = @"C:\Projects\coreclr\packages"; // "/git/coreclr/packages/";

        private const string PrivateCoreFxNetCoreAppVersion = "4.5.0-preview3-26427-0";
        private const string ExpectedLocalCoreFxVersion = "4.6.26427.0";

        private const string CoreClrVersion = "2.1.0-preview3-26420-0";
        private const string ExpectedLocalCoreClrVersion = "4.6.26420.0";

        public LocalCoreClrToolchainTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CanBenchmarkFullDotNetCoreStack()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreFxLocalBuild(PrivateCoreFxNetCoreAppVersion, CoreFxBinPackagesPath)
                        .UseCoreClrLocalBuild(CoreClrVersion, CoreClrBinPackagesPath, CoreClrPackagesPath)
                        .UseNuGetClearTag(false) // it should be removed after we add BDN to the dotnet/coreclr/dependencies.props file so all packages can be restored from CoreClrPackagesPath
                        .ToToolchain()));

            CanExecute<CheckLocalCoreClrAndCoreFxVersions>(config);
        }

        public class CheckLocalCoreClrAndCoreFxVersions : CheckCoreClrAndCoreFxVersions
        {
            public CheckLocalCoreClrAndCoreFxVersions() : base(expectedCoreClrVersion: ExpectedLocalCoreClrVersion, expectedCoreFxVersion: ExpectedLocalCoreFxVersion) { }
        }

        [Fact]
        public void CanBenchmarkLocalCoreClrAndMyGetCoreFx()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreFxNuGet("4.5.0-preview3-26403-04")
                        .UseCoreClrLocalBuild(CoreClrVersion, CoreClrBinPackagesPath, CoreClrPackagesPath)
                        .UseNuGetClearTag(false)
                        .ToToolchain()));

            CanExecute<CheckLocalCoreClrAnd_4_6_26403_04_CoreFxVersions>(config);
        }

        public class CheckLocalCoreClrAnd_4_6_26403_04_CoreFxVersions : CheckCoreClrAndCoreFxVersions
        {
            public CheckLocalCoreClrAnd_4_6_26403_04_CoreFxVersions() : base(expectedCoreClrVersion: ExpectedLocalCoreClrVersion, expectedCoreFxVersion: "4.6.26403.04") { }
        }

        [Fact]
        public void CanBenchmarkLocalCoreClrWithDefaultCoreFx()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreFxDefault()
                        .UseCoreClrLocalBuild(CoreClrVersion, CoreClrBinPackagesPath, CoreClrPackagesPath)
                        .UseNuGetClearTag(false)
                        .ToToolchain()));

            CanExecute<CheckLocalCoreClrVersion>(config);
        }

        public class CheckLocalCoreClrVersion : CheckCoreClrAndCoreFxVersions
        {
            public CheckLocalCoreClrVersion() : base(expectedCoreClrVersion: ExpectedLocalCoreClrVersion) { }
        }

        [Fact]
        public void CanBenchmarkLocalCoreFxWithDefaultCoreClr()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreFxLocalBuild(PrivateCoreFxNetCoreAppVersion, CoreFxBinPackagesPath)
                        .UseCoreClrDefault()
                        .UseNuGetClearTag(false)
                        .ToToolchain()));

            CanExecute<CheckLocalCoreFxVersion>(config);
        }

        public class CheckLocalCoreFxVersion : CheckCoreClrAndCoreFxVersions
        {
            public CheckLocalCoreFxVersion() : base(expectedCoreFxVersion: ExpectedLocalCoreFxVersion) { }
        }
    }
}
