using BenchmarkDotNet.Detectors.Cpu.Windows;
using Perfolizer.Phd.Dto;
using Xunit;
using Xunit.Abstractions;
using static Perfolizer.Horology.Frequency;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

// ReSharper disable StringLiteralTypo
public class WmicCpuInfoParserTests(ITestOutputHelper output)
{
    private ITestOutputHelper Output { get; } = output;

    [Fact]
    public void EmptyTest()
    {
        var actual = WmicCpuInfoParser.Parse(string.Empty);
        var expected = new PhdCpu();
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void MalformedTest()
    {
        var actual = WmicCpuInfoParser.Parse("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
        var expected = new PhdCpu();
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealTwoProcessorEightCoresTest()
    {
        const string cpuInfo = @"

MaxClockSpeed=2400
Name=Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz
NumberOfCores=8
NumberOfLogicalProcessors=16


MaxClockSpeed=2400
Name=Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz
NumberOfCores=8
NumberOfLogicalProcessors=16

";
        var actual = WmicCpuInfoParser.Parse(cpuInfo);
        var expected = new PhdCpu
        {
            ProcessorName = "Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz",
            PhysicalProcessorCount = 2,
            PhysicalCoreCount = 16,
            LogicalCoreCount = 32,
            NominalFrequencyHz = 2_400_000_000,
            MaxFrequencyHz = 2_400_000_000,
        };
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealTwoProcessorEightCoresWithWmicBugTest()
    {
        const string cpuInfo =
            "\r\r\n" +
            "\r\r\n" +
            "MaxClockSpeed=3111\r\r\n" +
            "Name=Intel(R) Xeon(R) CPU E5-2687W 0 @ 3.10GHz\r\r\n" +
            "NumberOfCores=8\r\r\n" +
            "NumberOfLogicalProcessors=16\r\r\n" +
            "\r\r\n" +
            "\r\r\n" +
            "MaxClockSpeed=3111\r\r\n" +
            "Name=Intel(R) Xeon(R) CPU E5-2687W 0 @ 3.10GHz\r\r\n" +
            "NumberOfCores=8\r\r\n" +
            "NumberOfLogicalProcessors=16\r\r\n" +
            "\r\r\n" +
            "\r\r\n" +
            "\r\r\n";
        var actual = WmicCpuInfoParser.Parse(cpuInfo);
        var expected = new PhdCpu
        {
            ProcessorName = "Intel(R) Xeon(R) CPU E5-2687W 0 @ 3.10GHz",
            PhysicalProcessorCount = 2,
            PhysicalCoreCount = 16,
            LogicalCoreCount = 32,
            NominalFrequencyHz = 3_111_000_000,
            MaxFrequencyHz = 3_111_000_000,
        };
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealOneProcessorFourCoresTest()
    {
        const string cpuInfo = @"

MaxClockSpeed=2500
Name=Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz
NumberOfCores=4
NumberOfLogicalProcessors=8

";

        var actual = WmicCpuInfoParser.Parse(cpuInfo);
        var expected = new PhdCpu
        {
            ProcessorName = "Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 4,
            LogicalCoreCount = 8,
            NominalFrequencyHz = 2_500_000_000,
            MaxFrequencyHz = 2_500_000_000,
        };
        Output.AssertEqual(expected, actual);
    }
}