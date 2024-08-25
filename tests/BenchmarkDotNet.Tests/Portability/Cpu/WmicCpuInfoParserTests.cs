using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Portability.Cpu.Windows;
using Xunit;
using Xunit.Abstractions;
using static Perfolizer.Horology.Frequency;

namespace BenchmarkDotNet.Tests.Portability.Cpu;

// ReSharper disable StringLiteralTypo
public class WmicCpuInfoParserTests(ITestOutputHelper output)
{
    private ITestOutputHelper Output { get; } = output;

    [Fact]
    public void EmptyTest()
    {
        var actual = WmicCpuInfoParser.Parse(string.Empty);
        var expected = CpuInfo.Empty;
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void MalformedTest()
    {
        var actual = WmicCpuInfoParser.Parse("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
        var expected = CpuInfo.Empty;
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
        var expected = new CpuInfo(
            "Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz",
            2, 16, 32, 2400 * MHz, 2400 * MHz);
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
        var expected = new CpuInfo(
            "Intel(R) Xeon(R) CPU E5-2687W 0 @ 3.10GHz",
            2, 16, 32, 3111 * MHz, 3111 * MHz);
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
        var expected = new CpuInfo(
            "Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz",
            1, 4, 8, 2500 * MHz, 2500 * MHz);
        Output.AssertEqual(expected, actual);
    }
}