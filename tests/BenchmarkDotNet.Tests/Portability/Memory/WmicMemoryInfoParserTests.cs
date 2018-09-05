using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability.Memory;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Memory
{
    public class WmicMemoryInfoParserTests
    {
        [Fact]
        public void EmptyTest()
        {
            var memoryInfo = WmicMemoryInfoParser.ParseOutput(string.Empty);
            Assert.Null(memoryInfo);
        }

        [Fact]
        public void MalformedTest()
        {
            var memoryInfo = WmicMemoryInfoParser.ParseOutput("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
            Assert.Null(memoryInfo);           
        }

        [Fact]
        public void RealMemoryTest()
        {
            string memoryInfo = @"

FreePhysicalMemory=2597508
TotalVisibleMemorySize=8309008



";
            var parsedMemoryInfo = WmicMemoryInfoParser.ParseOutput(memoryInfo);
            Assert.Equal(8309008, parsedMemoryInfo.TotalMemory);
            Assert.Equal(2597508, parsedMemoryInfo.FreePhysicalMemory);
        }
    }
}