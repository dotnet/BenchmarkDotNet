using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Portability.Cpu.Linux;
using Xunit;
using Xunit.Abstractions;
using static Perfolizer.Horology.Frequency;

namespace BenchmarkDotNet.Tests.Portability.Cpu;

// ReSharper disable StringLiteralTypo
public class LinuxCpuInfoParserTests(ITestOutputHelper output)
{
    private ITestOutputHelper Output { get; } = output;

    [Fact]
    public void EmptyTest()
    {
        var actual = LinuxCpuInfoParser.Parse("", "");
        var expected = CpuInfo.Empty;
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void MalformedTest()
    {
        var actual = LinuxCpuInfoParser.Parse("malformedkey: malformedvalue\n\nmalformedkey2: malformedvalue2", null);
        var expected = CpuInfo.Empty;
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void TwoProcessorWithDifferentCoresCountTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoProcessorWithDifferentCoresCount.txt");
        var actual = LinuxCpuInfoParser.Parse(cpuInfo, null);
        var expected = new CpuInfo(
            "Unknown processor with 2 cores and hyper threading, Unknown processor with 4 cores",
            2, 6, 8, 2.5 * GHz, 2.5 * GHz);
        Output.AssertEqual(expected, actual);
    }


    [Fact]
    public void RealOneProcessorTwoCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoRealOneProcessorTwoCores.txt");
        var actual = LinuxCpuInfoParser.Parse(cpuInfo, null);
        var expected = new CpuInfo(
            "Intel(R) Core(TM) i5-6200U CPU @ 2.30GHz",
            1, 2, 4, 2.3 * GHz, 2.3 * GHz);
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealOneProcessorFourCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoRealOneProcessorFourCores.txt");
        var actual = LinuxCpuInfoParser.Parse(cpuInfo, null);
        var expected = new CpuInfo(
            "Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz",
            1, 4, 8, 2.5 * GHz, 2.5 * GHz);
        Output.AssertEqual(expected, actual);
    }

    // https://github.com/dotnet/BenchmarkDotNet/issues/2577
    [Fact]
    public void Issue2577Test()
    {
        const string cpuInfo =
            """
            processor       : 0
            BogoMIPS        : 50.00
            Features        : fp asimd evtstrm aes pmull sha1 sha2 crc32 atomics fphp asimdhp cpuid asimdrdm lrcpc dcpop asimddp
            CPU implementer : 0x41
            CPU architecture: 8
            CPU variant     : 0x3
            CPU part        : 0xd0c
            CPU revision    : 1
            """;
        const string lscpu =
            """
            Architecture:           aarch64
              CPU op-mode(s):       32-bit, 64-bit
              Byte Order:           Little Endian
            CPU(s):                 16
              On-line CPU(s) list:  0-15
            Vendor ID:              ARM
              Model name:           Neoverse-N1
                Model:              1
                Thread(s) per core: 1
                Core(s) per socket: 16
                Socket(s):          1
                Stepping:           r3p1
                BogoMIPS:           50.00
                Flags:              fp asimd evtstrm aes pmull sha1 sha2 crc32 atomics fphp asimdhp cpuid asimdrdm lrcpc dcpop asimddp
            """;
        var actual = LinuxCpuInfoParser.Parse(cpuInfo, lscpu);
        var expected = new CpuInfo("Neoverse-N1", null, 16, null, null, null);
        Output.AssertEqual(expected, actual);
    }

    [Theory]
    [InlineData("Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz", 2.50)]
    [InlineData("Intel(R) Core(TM) i5-6200U CPU @ 2.30GHz", 2.30)]
    [InlineData("Unknown processor with 2 cores and hyper threading, Unknown processor with 4 cores", 0)]
    [InlineData("Intel(R) Core(TM) i5-2500 CPU @ 3.30GHz", 3.30)]
    public void ParseFrequencyFromBrandStringTests(string brandString, double expectedGHz)
    {
        var frequency = LinuxCpuInfoParser.ParseFrequencyFromBrandString(brandString) ?? Zero;
        Assert.Equal(FromGHz(expectedGHz), frequency);
    }
}