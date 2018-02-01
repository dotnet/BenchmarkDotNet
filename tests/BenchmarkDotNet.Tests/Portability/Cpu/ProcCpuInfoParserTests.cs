using BenchmarkDotNet.Portability.Cpu;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Cpu
{
    public class ProcCpuInfoParserTests
    {
        [Fact]
        public void EmptyTest()
        {
            var parser = ProcCpuInfoParser.ParseOutput(string.Empty);
            Assert.Null(parser.ProcessorName);
            Assert.Null(parser.PhysicalProcessorCount);
            Assert.Null(parser.PhysicalCoreCount);
            Assert.Null(parser.LogicalCoreCount);
        }

        [Fact]
        public void MalformedTest()
        {
            var parser = ProcCpuInfoParser.ParseOutput("malformedkey: malformedvalue\n\nmalformedkey2: malformedvalue2");
            Assert.Null(parser.ProcessorName);
            Assert.Null(parser.PhysicalProcessorCount);
            Assert.Null(parser.PhysicalCoreCount);
            Assert.Null(parser.LogicalCoreCount);
        }

        [Fact]
        public void TwoProcessorWithDifferentCoresCountTest()
        {
            string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoProcessorWithDifferentCoresCount.txt");
            var parser = ProcCpuInfoParser.ParseOutput(cpuInfo);
            Assert.Equal("Unknown processor with 2 cores and hyper threading, Unknown processor with 4 cores", parser.ProcessorName);
            Assert.Equal(2, parser.PhysicalProcessorCount);
            Assert.Equal(6, parser.PhysicalCoreCount);
            Assert.Equal(8, parser.LogicalCoreCount);
        }


        [Fact]
        public void RealOneProcessorTwoCoresTest()
        {
            string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoRealOneProcessorTwoCores.txt");
            var parser = ProcCpuInfoParser.ParseOutput(cpuInfo);
            Assert.Equal("Intel(R) Core(TM) i5-6200U CPU @ 2.30GHz", parser.ProcessorName);
            Assert.Equal(1, parser.PhysicalProcessorCount);
            Assert.Equal(2, parser.PhysicalCoreCount);
            Assert.Equal(4, parser.LogicalCoreCount);
        }

        [Fact]
        public void RealOneProcessorFourCoresTest()
        {
            string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoRealOneProcessorFourCores.txt");
            var parser = ProcCpuInfoParser.ParseOutput(cpuInfo);
            Assert.Equal("Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz", parser.ProcessorName);
            Assert.Equal(1, parser.PhysicalProcessorCount);
            Assert.Equal(4, parser.PhysicalCoreCount);
            Assert.Equal(8, parser.LogicalCoreCount);
        }
    }
}