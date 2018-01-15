using BenchmarkDotNet.Portability.Cpu;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability.Cpu
{
    public class WmicCpuInfoParserTests
    {
        [Fact]
        public void EmptyTest()
        {
            var parser = WmicCpuInfoParser.ParseOutput(string.Empty);
            Assert.Null(parser.ProcessorName);
            Assert.Null(parser.PhysicalProcessorCount);
            Assert.Null(parser.PhysicalCoreCount);
            Assert.Null(parser.LogicalCoreCount);
        }

        [Fact]
        public void MalformedTest()
        {
            var parser = WmicCpuInfoParser.ParseOutput("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
            Assert.Null(parser.ProcessorName);
            Assert.Null(parser.PhysicalProcessorCount);
            Assert.Null(parser.PhysicalCoreCount);
            Assert.Null(parser.LogicalCoreCount);
        }

        [Fact]
        public void RealTwoProcessorEightCoresTest()
        {
            var cpuInfo = @"

Name=Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz
NumberOfCores=8
NumberOfLogicalProcessors=16


Name=Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz
NumberOfCores=8
NumberOfLogicalProcessors=16

";
            var parser = WmicCpuInfoParser.ParseOutput(cpuInfo);
            Assert.Equal("Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz", parser.ProcessorName);
            Assert.Equal(2, parser.PhysicalProcessorCount);
            Assert.Equal(16, parser.PhysicalCoreCount);
            Assert.Equal(32, parser.LogicalCoreCount);
        }

        [Fact]
        public void RealOneProcessorFourCoresTest()
        {
            var cpuInfo = @"

Name=Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz
NumberOfCores=4
NumberOfLogicalProcessors=8

";

            var parser = WmicCpuInfoParser.ParseOutput(cpuInfo);
            Assert.Equal("Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz", parser.ProcessorName);
            Assert.Equal(1, parser.PhysicalProcessorCount);
            Assert.Equal(4, parser.PhysicalCoreCount);
            Assert.Equal(8, parser.LogicalCoreCount);
        }
    }
}