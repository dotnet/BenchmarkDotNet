using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Portability.Cpu.macOS;
using Xunit;
using Xunit.Abstractions;
using static Perfolizer.Horology.Frequency;

namespace BenchmarkDotNet.Tests.Portability.Cpu;

// ReSharper disable StringLiteralTypo
public class SysctlCpuInfoParserTests(ITestOutputHelper output)
{
    private ITestOutputHelper Output { get; } = output;

    [Fact]
    public void EmptyTest()
    {
        var actual = SysctlCpuInfoParser.Parse(string.Empty);
        var expected = CpuInfo.Empty;
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void MalformedTest()
    {
        var actual = SysctlCpuInfoParser.Parse("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
        var expected = CpuInfo.Empty;
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealOneProcessorFourCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("SysctlRealOneProcessorFourCores.txt");
        var actual = SysctlCpuInfoParser.Parse(cpuInfo);
        var expected = new CpuInfo(
            "Intel(R) Core(TM) i7-4770HQ CPU @ 2.20GHz",
            1, 4, 8, 2200 * MHz, 2200 * MHz);
        Output.AssertEqual(expected, actual);
    }
}