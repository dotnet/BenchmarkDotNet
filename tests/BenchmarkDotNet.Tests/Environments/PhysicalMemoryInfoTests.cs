using BenchmarkDotNet.Environments;
using Xunit;

namespace BenchmarkDotNet.Tests.Environments
{
    public class PhysicalMemoryInfoTests
    {
        [Fact]
        public void PhysicalMemoryInfo_ParsesAndConstructsCorrectly()
        {
            var memInfo = new PhysicalMemoryInfo(16000000000, null, 6400, "DDR4", 16);
            Assert.Equal(16000000000L, memInfo.TotalPhysicalBytes);
            Assert.Equal(6400L, memInfo.FrequencyMts);
            Assert.Equal("DDR4", memInfo.MemoryType);
            Assert.Equal((byte?)16, memInfo.CasLatency);
        }
    }
}
