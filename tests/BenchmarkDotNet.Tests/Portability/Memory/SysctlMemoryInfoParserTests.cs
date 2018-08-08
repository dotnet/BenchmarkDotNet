using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability.Memory;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Memory
{
    public class SysctlMemoryInfoParserTests
    {
        [Fact]
        public void EmptyTest()
        {
            var memoryInfo = SysctlMemoryInfoParser.ParseOutput(string.Empty);
            Assert.Null(memoryInfo);            
        }

        [Fact]
        public void MalformedTest()
        {
            var memoryInfo = SysctlMemoryInfoParser.ParseOutput("malformedkey:malformedvalue\n\nmalformedkey2=malformedvalue2");
            Assert.Null(memoryInfo);
        }

        [Fact]
        public void RealMemoryTest()
        {
            string MemoryInfo = TestHelper.ReadTestFile("SysctlMemory.txt", "Memory");
            var parsedMemoyInfo = SysctlMemoryInfoParser.ParseOutput(MemoryInfo);
            Assert.Equal(0, parsedMemoyInfo?.FreePhysicalMemory);
            Assert.Equal(17179869184, parsedMemoyInfo?.TotalMemory);
        }
    }
}