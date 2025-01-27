using BenchmarkDotNet.Detectors.Cpu.Linux;
using Perfolizer.Phd.Dto;
using Xunit;
using Xunit.Abstractions;
using static Perfolizer.Horology.Frequency;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

// ReSharper disable StringLiteralTypo
public class LinuxCpuInfoParserTests(ITestOutputHelper output)
{
    private ITestOutputHelper Output { get; } = output;

    [Fact]
    public void EmptyTest()
    {
        var actual = LinuxCpuInfoParser.Parse("", "");
        var expected = new PhdCpu();
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void MalformedTest()
    {
        var actual = LinuxCpuInfoParser.Parse("malformedkey: malformedvalue\n\nmalformedkey2: malformedvalue2", null);
        var expected = new PhdCpu();
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void TwoProcessorWithDifferentCoresCountTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoProcessorWithDifferentCoresCount.txt");
        var actual = LinuxCpuInfoParser.Parse(cpuInfo, null);
        var expected = new PhdCpu
        {
            ProcessorName = "Unknown processor with 2 cores and hyper threading, Unknown processor with 4 cores",
            PhysicalProcessorCount = 2,
            PhysicalCoreCount = 6,
            LogicalCoreCount = 8,
            NominalFrequencyHz = 2_500_000_000,
            MaxFrequencyHz = 2_500_000_000
        };
        Output.AssertEqual(expected, actual);
    }


    [Fact]
    public void RealOneProcessorTwoCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoRealOneProcessorTwoCores.txt");
        var actual = LinuxCpuInfoParser.Parse(cpuInfo, null);
        var expected = new PhdCpu
        {
            ProcessorName = "Intel(R) Core(TM) i5-6200U CPU @ 2.30GHz",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 2,
            LogicalCoreCount = 4,
            NominalFrequencyHz = 2_300_000_000,
            MaxFrequencyHz = 2_300_000_000
        };
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealOneProcessorFourCoresTest()
    {
        string cpuInfo = TestHelper.ReadTestFile("ProcCpuInfoRealOneProcessorFourCores.txt");
        var actual = LinuxCpuInfoParser.Parse(cpuInfo, null);
        var expected = new PhdCpu
        {
            ProcessorName = "Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 4,
            LogicalCoreCount = 8,
            NominalFrequencyHz = 2_500_000_000,
            MaxFrequencyHz = 2_500_000_000
        };
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
        var expected = new PhdCpu { ProcessorName = "Neoverse-N1", PhysicalCoreCount = 16 };
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void AmdRyzen9_7950X()
    {
        string cpuInfo = TestHelper.ReadTestFile("ryzen9-cpuinfo.txt");
        const string lscpu =
            """
            Architecture:             x86_64
              CPU op-mode(s):         32-bit, 64-bit
              Address sizes:          48 bits physical, 48 bits virtual
              Byte Order:             Little Endian
            CPU(s):                   32
              On-line CPU(s) list:    0-31
            Vendor ID:                AuthenticAMD
              Model name:             AMD Ryzen 9 7950X 16-Core Processor
                CPU family:           25
                Model:                97
                Thread(s) per core:   2
                Core(s) per socket:   16
                Socket(s):            1
                Stepping:             2
                CPU(s) scaling MHz:   41%
                CPU max MHz:          5881.0000
                CPU min MHz:          400.0000
                BogoMIPS:             8983.23
                Flags:                fpu vme de pse tsc msr pae mce cx8 apic sep mtrr pge mca cmov pat pse36 clflush mmx fxsr sse sse2 ht syscall nx mmxext fxsr_opt pdpe1gb rdtscp lm constant_tsc rep_good amd_lbr_v2 nopl nonstop_tsc cpuid extd_apicid aperfmperf rapl 
                                      pni pclmulqdq monitor ssse3 fma cx16 sse4_1 sse4_2 x2apic movbe popcnt aes xsave avx f16c rdrand lahf_lm cmp_legacy svm extapic cr8_legacy abm sse4a misalignsse 3dnowprefetch osvw ibs skinit wdt tce topoext perfctr_core perfctr_nb
                                       bpext perfctr_llc mwaitx cpb cat_l3 cdp_l3 hw_pstate ssbd mba perfmon_v2 ibrs ibpb stibp ibrs_enhanced vmmcall fsgsbase bmi1 avx2 smep bmi2 erms invpcid cqm rdt_a avx512f avx512dq rdseed adx smap avx512ifma clflushopt clwb avx512
                                      cd sha_ni avx512bw avx512vl xsaveopt xsavec xgetbv1 xsaves cqm_llc cqm_occup_llc cqm_mbm_total cqm_mbm_local user_shstk avx512_bf16 clzero irperf xsaveerptr rdpru wbnoinvd cppc arat npt lbrv svm_lock nrip_save tsc_scale vmcb_clean
                                       flushbyasid decodeassists pausefilter pfthreshold avic v_vmsave_vmload vgif x2avic v_spec_ctrl vnmi avx512vbmi umip pku ospke avx512_vbmi2 gfni vaes vpclmulqdq avx512_vnni avx512_bitalg avx512_vpopcntdq rdpid overflow_recov succo
                                      r smca fsrm flush_l1d
            """;
        var actual = LinuxCpuInfoParser.Parse(cpuInfo, lscpu);
        var expected = new PhdCpu
        {
            ProcessorName = "AMD Ryzen 9 7950X 16-Core Processor",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 16,
            LogicalCoreCount = 32,
            NominalFrequencyHz = 5_881_000_000,
            MaxFrequencyHz = 5_881_000_000
        };
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