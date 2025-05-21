using BenchmarkDotNet.Detectors.Cpu.macOS;
using Perfolizer.Models;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

// ReSharper disable StringLiteralTypo
public class SysctlCpuInfoParserTests(ITestOutputHelper output)
{
    private ITestOutputHelper Output { get; } = output;

    [Fact]
    public void EmptyTest()
    {
        var actual = SysctlCpuInfoParser.Parse(string.Empty);
        var expected = new CpuInfo();
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void MalformedTest()
    {
        var actual = SysctlCpuInfoParser.Parse("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
        var expected = new CpuInfo();
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealOneProcessorFourCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("SysctlRealOneProcessorFourCores.txt");
        var actual = SysctlCpuInfoParser.Parse(cpuInfo);
        var expected = new CpuInfo
        {
            ProcessorName = "Intel(R) Core(TM) i7-4770HQ CPU @ 2.20GHz",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 4,
            LogicalCoreCount = 8,
            NominalFrequencyHz = 2_200_000_000,
            MaxFrequencyHz = 2_200_000_000
        };
        Output.AssertEqual(expected, actual);
    }
}