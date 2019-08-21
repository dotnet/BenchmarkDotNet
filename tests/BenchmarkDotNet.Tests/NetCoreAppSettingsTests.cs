using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.DotNetCli;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class NetCoreAppSettingsTests
    {
        [Fact]
        public void IsOlderThanWorksAsExpected()
        {
            Assert.True(NetCoreAppSettings.NetCoreApp20.IsOlderThan(TargetFrameworkMoniker.NetCoreApp30));
            Assert.True(NetCoreAppSettings.NetCoreApp21.IsOlderThan(TargetFrameworkMoniker.NetCoreApp30));
            Assert.True(NetCoreAppSettings.NetCoreApp22.IsOlderThan(TargetFrameworkMoniker.NetCoreApp30));

            Assert.False(NetCoreAppSettings.NetCoreApp30.IsOlderThan(TargetFrameworkMoniker.NetCoreApp30));
            Assert.True(NetCoreAppSettings.NetCoreApp30.IsOlderThan(TargetFrameworkMoniker.NetCoreApp31));
            Assert.True(NetCoreAppSettings.NetCoreApp30.IsOlderThan(TargetFrameworkMoniker.NetCoreApp50));
        }
    }
}
