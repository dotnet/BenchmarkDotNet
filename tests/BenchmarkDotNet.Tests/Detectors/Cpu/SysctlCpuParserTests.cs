using BenchmarkDotNet.Detectors.Cpu;
using Perfolizer.Horology;
using Xunit;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

public class SysctlCpuParserTests
{
    [Fact]
    public void EmptyTest()
    {
        var parser = SysctlCpuParser.Parse(string.Empty);
        Assert.Null(parser.ProcessorName);
        Assert.Null(parser.PhysicalProcessorCount);
        Assert.Null(parser.PhysicalCoreCount);
        Assert.Null(parser.LogicalCoreCount);
        Assert.Null(parser.NominalFrequencyHz);
    }

    [Fact]
    public void MalformedTest()
    {
        var parser = SysctlCpuParser.Parse("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
        Assert.Null(parser.ProcessorName);
        Assert.Null(parser.PhysicalProcessorCount);
        Assert.Null(parser.PhysicalCoreCount);
        Assert.Null(parser.LogicalCoreCount);
        Assert.Null(parser.NominalFrequencyHz);
    }

    [Fact]
    public void RealOneProcessorFourCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("SysctlRealOneProcessorFourCores.txt");
        var parser = SysctlCpuParser.Parse(cpuInfo);
        Assert.Equal("Intel(R) Core(TM) i7-4770HQ CPU @ 2.20GHz", parser.ProcessorName);
        Assert.Equal(1, parser.PhysicalProcessorCount);
        Assert.Equal(4, parser.PhysicalCoreCount);
        Assert.Equal(8, parser.LogicalCoreCount);
        Assert.Equal(2200 * Frequency.MHz, parser.GetNominalFrequency());
    }
}