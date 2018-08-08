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
        public void RealMemoryTest()
        {
            string MemoryInfo = @"MemTotal:        8309008 kB
MemFree:         2513996 kB
HighTotal:             0 kB
HighFree:              0 kB
LowTotal:        8309008 kB
LowFree:         2513996 kB
SwapTotal:       5060300 kB
SwapFree:        3334460 kB
";
            var parsedMemoyInfo = ProcMemoryInfoParser.ParseOutput(MemoryInfo);
            Assert.Equal(2513996L, parsedMemoyInfo.FreePhysicalMemory);
            Assert.Equal(8309008L, parsedMemoyInfo.TotalMemory);
        }
    }
}