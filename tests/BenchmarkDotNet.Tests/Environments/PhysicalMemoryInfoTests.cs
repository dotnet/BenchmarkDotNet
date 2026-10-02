using BenchmarkDotNet.Environments;
using Xunit;

namespace BenchmarkDotNet.Tests.Environments
{
    public class PhysicalMemoryInfoTests
    {
        [Fact]
        public void PhysicalMemoryInfo_ParsesAndConstructsCorrectly()
        {
            var memInfo = new PhysicalMemoryInfo(16000000000, null, 3200, "DDR4");
            Assert.Equal(16000000000L, memInfo.TotalPhysicalBytes);
            Assert.Equal(3200L, memInfo.FrequencyMHz);
            Assert.Equal("DDR4", memInfo.MemoryType);
        }
    }
}
