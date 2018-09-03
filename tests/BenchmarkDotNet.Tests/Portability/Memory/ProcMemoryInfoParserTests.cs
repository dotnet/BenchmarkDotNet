using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability.Memory;
using BenchmarkDotNet.Tests.Environments;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Memory
{
    public class ProcMemoryInfoParserTests
    {
        [Fact]
        public void EmptyTest()
        {
            var memoryInfo = ProcMemoryInfoParser.ParseOutput(string.Empty);
            Assert.Null(memoryInfo);
        }

        [Fact]
        public void MalformedTest()
        {
            var memoryInfo = ProcMemoryInfoParser.ParseOutput("malformedkey: malformedvalue\n\nmalformedkey2: malformedvalue2");
            Assert.Null(memoryInfo);
        }

        [Fact]
        public void RealMemoryTestWithAvaibleMemory()
        {            
            string MemoryInfo = TestHelper.ReadTestFile("ProcMemoryWithMemAvailable.txt", "Memory");
            var parsedMemoryInfo = ProcMemoryInfoParser.ParseOutput(MemoryInfo);
            Assert.Equal(6204328L, parsedMemoryInfo.FreePhysicalMemory);
            Assert.Equal(32904420L, parsedMemoryInfo.TotalMemory);
        }

        [Fact]
        public void RealMemoryTestWithoutAvailableMemory()
        {
            string MemoryInfo = TestHelper.ReadTestFile("ProcMemoryWithoutMemAvailable.txt", "Memory");
            var parsedMemoryInfo = ProcMemoryInfoParser.ParseOutput(MemoryInfo);
            Assert.Equal(4259532L, parsedMemoryInfo.FreePhysicalMemory);
            Assert.Equal(32904420L, parsedMemoryInfo.TotalMemory);
        }
    }
}