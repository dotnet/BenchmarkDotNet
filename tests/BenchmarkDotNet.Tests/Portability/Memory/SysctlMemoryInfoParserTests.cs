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
            Assert.Null(SysctlMemoryInfoParser.ParseOutput(string.Empty, null));
            Assert.Null(SysctlMemoryInfoParser.ParseOutput(null, string.Empty));            
        }

        [Fact]
        public void MalformedTest()
        {
            var memoryInfo = SysctlMemoryInfoParser.ParseOutput("malformedkey:malformedvalue\n\nmalformedkey2=malformedvalue2", "malformedkey:malformedvalue\n\nmalformedkey2=malformedvalue2");
            Assert.Null(memoryInfo);
        }

        [Fact]
        public void RealMemoryTest()
        {
            string sysctlMemoryInfo = TestHelper.ReadTestFile("SysctlMemory.txt", "Memory");
            string vmstatMemoryInfo = TestHelper.ReadTestFile("VmStatMemory.txt", "Memory");
            var parsedMemoyInfo = SysctlMemoryInfoParser.ParseOutput(sysctlMemoryInfo, vmstatMemoryInfo);
            Assert.Equal(5538, parsedMemoyInfo?.FreePhysicalMemory);
            Assert.Equal(17179869184, parsedMemoyInfo?.TotalMemory);
        }
    }
}