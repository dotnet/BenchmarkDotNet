using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Detectors;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class RuntimeVersionDetectionTests
    {
        [Theory]
        [InlineData(".NETCoreApp,Version=v2.0", RuntimeMoniker.NetCoreApp20, "netcoreapp2.0")]
        [InlineData(".NETCoreApp,Version=v2.1", RuntimeMoniker.NetCoreApp21, "netcoreapp2.1")]
        [InlineData(".NETCoreApp,Version=v2.2", RuntimeMoniker.NetCoreApp22, "netcoreapp2.2")]
        [InlineData(".NETCoreApp,Version=v3.0", RuntimeMoniker.NetCoreApp30, "netcoreapp3.0")]
        [InlineData(".NETCoreApp,Version=v3.1", RuntimeMoniker.NetCoreApp31, "netcoreapp3.1")]
        [InlineData(".NETCoreApp,Version=v5.0", RuntimeMoniker.Net50, "net5.0")]
        [InlineData(".NETCoreApp,Version=v6.0", RuntimeMoniker.Net60, "net6.0")]
        [InlineData(".NETCoreApp,Version=v7.0", RuntimeMoniker.Net70, "net7.0")]
        [InlineData(".NETCoreApp,Version=v8.0", RuntimeMoniker.Net80, "net8.0")]
        [InlineData(".NETCoreApp,Version=v9.0", RuntimeMoniker.Net90, "net9.0")]
        [InlineData(".NETCoreApp,Version=v10.0", RuntimeMoniker.Net10_0, "net10.0")]
        [InlineData(".NETCoreApp,Version=v11.0", RuntimeMoniker.Net11_0, "net11.0")]
        [InlineData(".NETCoreApp,Version=v123.0", RuntimeMoniker.NotRecognized, "net123.0")]
        public void TryGetVersionFromFrameworkNameHandlesValidInput(string frameworkName, RuntimeMoniker expectedTfm, string expectedMsBuildMoniker)
        {
            Assert.True(CoreRuntime.TryGetVersionFromFrameworkName(frameworkName, out Version version));

            var runtime = CoreRuntime.FromVersion(version);

            Assert.Equal(expectedTfm, runtime.RuntimeMoniker);
            Assert.Equal(expectedMsBuildMoniker, runtime.MsBuildMoniker);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(".NETCoreApp,Version=v")]
        [InlineData("just wrong")]
        public void TryGetVersionFromFrameworkNameHandlesInvalidInput(string? frameworkName)
        {
            Assert.False(CoreRuntime.TryGetVersionFromFrameworkName(frameworkName, out _));
        }

        [Theory]
        [InlineData(RuntimeMoniker.NetCoreApp21, "netcoreapp2.1", "Microsoft .NET Framework", "4.6.27817.01 @BuiltBy: dlab14-DDVSOWINAGE101 @Branch: release/2.1 @SrcCode: https://github.com/dotnet/coreclr/tree/6f78fbb3f964b4f407a2efb713a186384a167e5c")]
        [InlineData(RuntimeMoniker.NetCoreApp22, "netcoreapp2.2", "Microsoft .NET Framework", "4.6.27817.03 @BuiltBy: dlab14-DDVSOWINAGE101 @Branch: release/2.2 @SrcCode: https://github.com/dotnet/coreclr/tree/ce1d090d33b400a25620c0145046471495067cc7")]
        [InlineData(RuntimeMoniker.NetCoreApp30, "netcoreapp3.0", "Microsoft .NET Core", "3.0.0-preview8-28379-12")]
        [InlineData(RuntimeMoniker.NetCoreApp31, "netcoreapp3.1", "Microsoft .NET Core", "3.1.0-something")]
        [InlineData(RuntimeMoniker.Net50, "net5.0", "Microsoft .NET Core", "5.0.0-alpha1.19415.3")]
        [InlineData(RuntimeMoniker.NotRecognized, "net123.0", "Microsoft .NET Core", "123.0.0-future")]
        public void TryGetVersionFromProductInfoHandlesValidInput(RuntimeMoniker expectedTfm, string expectedMsBuildMoniker, string productName, string productVersion)
        {
            Assert.True(CoreRuntime.TryGetVersionFromProductInfo(productVersion, productName, out Version version));

            var runtime = CoreRuntime.FromVersion(version);

            Assert.Equal(expectedTfm, runtime.RuntimeMoniker);
            Assert.Equal(expectedMsBuildMoniker, runtime.MsBuildMoniker);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("not", "ok")]
        [InlineData("Microsoft .NET Framework", "4.6.26614.01 @BuiltBy: dlab14-DDVSOWINAGE018 @Commit: a536e7eec55c538c94639cefe295aa672996bf9b")] // this is an actual output for 2.0 but it simply does not contain enough info
        public void TryGetVersionFromProductInfoHandlesInvalidInput(string? productName, string? productVersion)
        {
            Assert.False(CoreRuntime.TryGetVersionFromProductInfo(productVersion, productName, out _));
        }

        public static IEnumerable<object[]> FromNetCoreAppVersionHandlesValidInputArguments()
        {
            string directoryPrefix = Path.GetTempPath(); // this test runs on Unix, it can not be hardcoded due to / \ difference

            yield return new object[] { Path.Combine(directoryPrefix, "2.0.9") + Path.DirectorySeparatorChar, RuntimeMoniker.NetCoreApp20, "netcoreapp2.0" };
            yield return new object[] { Path.Combine(directoryPrefix, "2.1.12") + Path.DirectorySeparatorChar, RuntimeMoniker.NetCoreApp21, "netcoreapp2.1" };
            yield return new object[] { Path.Combine(directoryPrefix, "2.2.6") + Path.DirectorySeparatorChar, RuntimeMoniker.NetCoreApp22, "netcoreapp2.2" };
            yield return new object[] { Path.Combine(directoryPrefix, "3.0.0-preview8-28379-12") + Path.DirectorySeparatorChar, RuntimeMoniker.NetCoreApp30, "netcoreapp3.0" };
            yield return new object[] { Path.Combine(directoryPrefix, "5.0.0-alpha1.19422.13") + Path.DirectorySeparatorChar, RuntimeMoniker.Net50, "net5.0" };
            yield return new object[] { Path.Combine(directoryPrefix, "123.0.0") + Path.DirectorySeparatorChar, RuntimeMoniker.NotRecognized, "net123.0" };
        }

        [Theory]
        [MemberData(nameof(FromNetCoreAppVersionHandlesValidInputArguments))]
        public void TryGetVersionFromRuntimeDirectoryHandlesValidInput(string runtimeDirectory, RuntimeMoniker expectedTfm, string expectedMsBuildMoniker)
        {
            Assert.True(CoreRuntime.TryGetVersionFromRuntimeDirectory(runtimeDirectory, out Version version));

            var runtime = CoreRuntime.FromVersion(version);

            Assert.Equal(expectedTfm, runtime.RuntimeMoniker);
            Assert.Equal(expectedMsBuildMoniker, runtime.MsBuildMoniker);
        }

        public static IEnumerable<object[]> TryGetVersionFromRuntimeDirectoryInvalidInputArguments()
        {
            yield return new object[] { null };
            yield return new object[] { string.Empty };
            yield return new object[] { Path.Combine(Path.GetTempPath(), "publish") };
            yield return new object[] { Path.Combine(Path.GetTempPath(), "publish") + Path.DirectorySeparatorChar };
        }

        [Theory]
        [MemberData(nameof(TryGetVersionFromRuntimeDirectoryInvalidInputArguments))]
        public void TryGetVersionFromRuntimeDirectoryHandlesInvalidInput(string runtimeDirectory)
        {
            Assert.False(CoreRuntime.TryGetVersionFromRuntimeDirectory(runtimeDirectory, out _));
        }

        [Fact]
        public void CurrentRuntimeIsProperlyRecognized()
        {
            var runtime = RuntimeInformation.GetCurrentRuntime();

#if NETFRAMEWORK
            if (OsDetector.IsWindows())
                Assert.True(runtime is ClrRuntime, $"Actual runtime: {runtime}, tfm: {runtime.MsBuildMoniker}, moniker: {runtime.RuntimeMoniker}");
            else
                Assert.True(runtime is MonoRuntime, $"Actual runtime: {runtime}, tfm: {runtime.MsBuildMoniker}, moniker: {runtime.RuntimeMoniker}");
#else
            if (DotNetRuntimeHelper.GetExpectedDotNetCoreRuntimeName() == ".NET 10.0")
                Assert.True(runtime is CoreRuntime coreRuntime && coreRuntime.RuntimeMoniker == RuntimeMoniker.Net10_0, $"Actual runtime: {runtime}, tfm: {runtime.MsBuildMoniker}, moniker: {runtime.RuntimeMoniker}");
            else
                Assert.True(runtime is CoreRuntime coreRuntime && coreRuntime.RuntimeMoniker == RuntimeMoniker.Net80, $"Actual runtime: {runtime}, tfm: {runtime.MsBuildMoniker}, moniker: {runtime.RuntimeMoniker}");
#endif
        }
    }
}
