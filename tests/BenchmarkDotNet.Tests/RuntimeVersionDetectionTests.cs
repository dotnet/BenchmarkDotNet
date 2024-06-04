using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class RuntimeVersionDetectionTests
    {
        [Theory]
        [InlineData(".NETCoreApp,Version=v3.1", RuntimeMoniker.NetCoreApp31, "netcoreapp3.1")]
        [InlineData(".NETCoreApp,Version=v5.0", RuntimeMoniker.Net50, "net5.0")]
        [InlineData(".NETCoreApp,Version=v6.0", RuntimeMoniker.Net60, "net6.0")]
        [InlineData(".NETCoreApp,Version=v7.0", RuntimeMoniker.Net70, "net7.0")]
        [InlineData(".NETCoreApp,Version=v8.0", RuntimeMoniker.Net80, "net8.0")]
        [InlineData(".NETCoreApp,Version=v9.0", RuntimeMoniker.Net90, "net9.0")]
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
            if (RuntimeInformation.IsWindows())
                Assert.True(runtime is ClrRuntime);
            else
                Assert.True(runtime is MonoRuntime);
#elif NETCOREAPP3_1
            Assert.True(runtime is CoreRuntime coreRuntime && coreRuntime.RuntimeMoniker == RuntimeMoniker.NetCoreApp31);
#elif NETCOREAPP5_0
            Assert.True(runtime is CoreRuntime coreRuntime && coreRuntime.RuntimeMoniker == RuntimeMoniker.NetCoreApp50);
#elif NET8_0
            Assert.True(runtime is CoreRuntime coreRuntime && coreRuntime.RuntimeMoniker == RuntimeMoniker.Net80);
#endif
        }
    }
}
