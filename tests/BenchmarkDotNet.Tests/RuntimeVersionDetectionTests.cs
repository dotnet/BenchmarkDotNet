using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class RuntimeVersionDetectionTests
    {
        [Theory]
        [InlineData("2.0", TargetFrameworkMoniker.NetCoreApp20, "netcoreapp2.0")]
        [InlineData("2.1", TargetFrameworkMoniker.NetCoreApp21, "netcoreapp2.1")]
        [InlineData("2.2", TargetFrameworkMoniker.NetCoreApp22, "netcoreapp2.2")]
        [InlineData("3.0", TargetFrameworkMoniker.NetCoreApp30, "netcoreapp3.0")]
        [InlineData("3.1", TargetFrameworkMoniker.NetCoreApp31, "netcoreapp3.1")]
        [InlineData("5.0", TargetFrameworkMoniker.NetCoreApp50, "netcoreapp5.0")]
        [InlineData("123.0", TargetFrameworkMoniker.NotRecognized, "netcoreapp123.0")]
        public void FromNetCoreAppVersionParsesVersionProperly(string netCoreAppVersion, TargetFrameworkMoniker expectedTfm, string expectedMsBuildMoniker)
        {
            var runtime = CoreRuntime.FromNetCoreAppVersion(netCoreAppVersion);

            Assert.Equal(expectedTfm, runtime.TargetFrameworkMoniker);
            Assert.Equal(expectedMsBuildMoniker, runtime.MsBuildMoniker);
        }

        [Theory]
        [InlineData(TargetFrameworkMoniker.NetCoreApp20, "netcoreapp2.0", "Microsoft .NET Framework", "4.6.26614.01 @BuiltBy: dlab14-DDVSOWINAGE018 @Commit: a536e7eec55c538c94639cefe295aa672996bf9b")]
        [InlineData(TargetFrameworkMoniker.NetCoreApp21, "netcoreapp2.1", "Microsoft .NET Framework", "4.6.27817.01 @BuiltBy: dlab14-DDVSOWINAGE101 @Branch: release/2.1 @SrcCode: https://github.com/dotnet/coreclr/tree/6f78fbb3f964b4f407a2efb713a186384a167e5c")]
        [InlineData(TargetFrameworkMoniker.NetCoreApp22, "netcoreapp2.2", "Microsoft .NET Framework", "4.6.27817.03 @BuiltBy: dlab14-DDVSOWINAGE101 @Branch: release/2.2 @SrcCode: https://github.com/dotnet/coreclr/tree/ce1d090d33b400a25620c0145046471495067cc7")]
        [InlineData(TargetFrameworkMoniker.NetCoreApp30, "netcoreapp3.0", "Microsoft .NET Core", "3.0.0-preview8-28379-12")]
        [InlineData(TargetFrameworkMoniker.NetCoreApp31, "netcoreapp3.1", "Microsoft .NET Core", "3.1.0-something")]
        [InlineData(TargetFrameworkMoniker.NetCoreApp50, "netcoreapp5.0", "Microsoft .NET Core", "5.0.0-alpha1.19415.3")]
        [InlineData(TargetFrameworkMoniker.NotRecognized, "netcoreapp123.0", "Microsoft .NET Core", "123.0.0-future")]
        public void FromProductVersionParsesVersionProperly(TargetFrameworkMoniker expectedTfm, string expectedMsBuildMoniker, string productName, string productVersion)
        {
            var runtime = CoreRuntime.FromProductVersion(productVersion, productName);

            Assert.Equal(expectedTfm, runtime.TargetFrameworkMoniker);
            Assert.Equal(expectedMsBuildMoniker, runtime.MsBuildMoniker);
        }

        [Fact]
        public void CurrentRuntimeIsProperlyRecognized()
        {
            var runtime = RuntimeInformation.GetCurrentRuntime();

#if NETFRAMEWORK
            Assert.True(runtime is ClrRuntime);
#elif NETCOREAPP2_1
            Assert.True(runtime is CoreRuntime coreRuntime && coreRuntime.TargetFrameworkMoniker == TargetFrameworkMoniker.NetCoreApp21);
#endif
        }
    }
}
