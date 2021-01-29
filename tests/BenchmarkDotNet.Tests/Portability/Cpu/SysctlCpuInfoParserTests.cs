using BenchmarkDotNet.Portability.Cpu;
using Perfolizer.Horology;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Cpu
{
    public class SysctlCpuInfoParserTests
    {
        [Fact]
        public void EmptyTest()
        {
            var parser = SysctlCpuInfoParser.ParseOutput(string.Empty);
            Assert.Null(parser.ProcessorName);
            Assert.Null(parser.PhysicalProcessorCount);
            Assert.Null(parser.PhysicalCoreCount);
            Assert.Null(parser.LogicalCoreCount);
            Assert.Null(parser.NominalFrequency);
        }

        [Fact]
        public void MalformedTest()
        {
            var parser = SysctlCpuInfoParser.ParseOutput("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
            Assert.Null(parser.ProcessorName);
            Assert.Null(parser.PhysicalProcessorCount);
            Assert.Null(parser.PhysicalCoreCount);
            Assert.Null(parser.LogicalCoreCount);
            Assert.Null(parser.NominalFrequency);
        }

        [Fact]
        public void RealOneProcessorFourCoresTest()
        {
            string cpuInfo = TestHelper.ReadTestFile("SysctlRealOneProcessorFourCores.txt");
            var parser = SysctlCpuInfoParser.ParseOutput(cpuInfo);
            Assert.Equal("Intel(R) Core(TM) i7-4770HQ CPU @ 2.20GHz", parser.ProcessorName);
            Assert.Equal(1, parser.PhysicalProcessorCount);
            Assert.Equal(4, parser.PhysicalCoreCount);
            Assert.Equal(8, parser.LogicalCoreCount);
            Assert.Equal(2200 * Frequency.MHz, parser.NominalFrequency);
        }
    }
}