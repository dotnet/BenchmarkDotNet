using BenchmarkDotNet.Detectors.Cpu;
using Perfolizer.Horology;
using Xunit;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

public class ProcCpuParserTests
{
    [Fact]
    public void EmptyTest()
    {
        var parser = ProcCpuParser.Parse(string.Empty);
        Assert.Null(parser.ProcessorName);
        Assert.Null(parser.PhysicalProcessorCount);
        Assert.Null(parser.PhysicalCoreCount);
        Assert.Null(parser.LogicalCoreCount);
        Assert.Null(parser.NominalFrequencyHz);
        Assert.Null(parser.MinFrequencyHz);
        Assert.Null(parser.MaxFrequencyHz);
    }

    [Fact]
    public void MalformedTest()
    {
        var parser = ProcCpuParser.Parse("malformedkey: malformedvalue\n\nmalformedkey2: malformedvalue2");
        Assert.Null(parser.ProcessorName);
        Assert.Null(parser.PhysicalProcessorCount);
        Assert.Null(parser.PhysicalCoreCount);
        Assert.Null(parser.LogicalCoreCount);
        Assert.Null(parser.NominalFrequencyHz);
        Assert.Null(parser.MaxFrequencyHz);
        Assert.Null(parser.MinFrequencyHz);
    }

    [Fact]
    public void TwoProcessorWithDifferentCoresCountTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoProcessorWithDifferentCoresCount.txt");
        var parser = ProcCpuParser.Parse(cpuInfo);
        Assert.Equal("Unknown processor with 2 cores and hyper threading, Unknown processor with 4 cores", parser.ProcessorName);
        Assert.Equal(2, parser.PhysicalProcessorCount);
        Assert.Equal(6, parser.PhysicalCoreCount);
        Assert.Equal(8, parser.LogicalCoreCount);
        Assert.Null(parser.NominalFrequencyHz);
        Assert.Equal(0.8 * Frequency.GHz, parser.GetMinFrequency());
        Assert.Equal(2.5 * Frequency.GHz, parser.GetMaxFrequency());
    }


    [Fact]
    public void RealOneProcessorTwoCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoRealOneProcessorTwoCores.txt");
        var parser = ProcCpuParser.Parse(cpuInfo);
        Assert.Equal("Intel(R) Core(TM) i5-6200U CPU @ 2.30GHz", parser.ProcessorName);
        Assert.Equal(1, parser.PhysicalProcessorCount);
        Assert.Equal(2, parser.PhysicalCoreCount);
        Assert.Equal(4, parser.LogicalCoreCount);
        Assert.Equal(2.3 * Frequency.GHz, parser.GetNominalFrequency());
        Assert.Equal(0.8 * Frequency.GHz, parser.GetMinFrequency());
        Assert.Equal(2.3 * Frequency.GHz, parser.GetMaxFrequency());
    }

    [Fact]
    public void RealOneProcessorFourCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoRealOneProcessorFourCores.txt");
        var parser = ProcCpuParser.Parse(cpuInfo);
        Assert.Equal("Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz", parser.ProcessorName);
        Assert.Equal(1, parser.PhysicalProcessorCount);
        Assert.Equal(4, parser.PhysicalCoreCount);
        Assert.Equal(8, parser.LogicalCoreCount);
        Assert.Equal(2.5 * Frequency.GHz, parser.GetNominalFrequency());
        Assert.Equal(0.8 * Frequency.GHz, parser.GetMinFrequency());
        Assert.Equal(2.5 * Frequency.GHz, parser.GetMaxFrequency());
    }

    [Theory]
    [InlineData("Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz", 2.50)]
    [InlineData("Intel(R) Core(TM) i5-6200U CPU @ 2.30GHz", 2.30)]
    [InlineData("Unknown processor with 2 cores and hyper threading, Unknown processor with 4 cores", 0)]
    [InlineData("Intel(R) Core(TM) i5-2500 CPU @ 3.30GHz", 3.30)]
    public void ParseFrequencyFromBrandStringTests(string brandString, double expectedGHz)
    {
        var frequency = ProcCpuParser.ParseFrequencyFromBrandString(brandString);
        Assert.Equal(Frequency.FromGHz(expectedGHz), frequency);
    }
}