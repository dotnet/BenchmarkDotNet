using BenchmarkDotNet.Detectors.Cpu;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

public class PerformanceCoresDetectorTests
{
    [Fact]
    public void MaskContainsOnlyTheProcessorsWithTheHighestRank()
    {
        // 4 P-cores with hyper-threading (0-7) and 4 E-cores (8-11), like an Intel hybrid CPU
        var processors = Enumerable.Range(0, 8).Select(cpu => (cpu, 1))
            .Concat(Enumerable.Range(8, 4).Select(cpu => (cpu, 0)));

        Assert.Equal(0xFFUL, PerformanceCoresDetector.BuildMask(processors));
    }

    [Fact]
    public void MaskSupportsMoreThanTwoCoreTypes()
    {
        // Arm DynamIQ: little (0-3), big (4-6) and prime (7) cores
        (int, int)[] processors = [(0, 160), (1, 160), (2, 160), (3, 160), (4, 768), (5, 768), (6, 768), (7, 1024)];

        Assert.Equal(1UL << 7, PerformanceCoresDetector.BuildMask(processors));
    }

    [Fact]
    public void MaskSupportsTheSixtyFourthProcessorAndIgnoresTheOnesBeyond()
    {
        (int, int)[] processors = [(0, 0), (63, 1), (64, 1), (100, 1)];

        Assert.Equal(1UL << 63, PerformanceCoresDetector.BuildMask(processors));
    }

    [Fact]
    public void HomogeneousCpuHasNoPerformanceCoresMask()
    {
        Assert.Null(PerformanceCoresDetector.BuildMask(Enumerable.Range(0, 8).Select(cpu => (cpu, 1024))));
        Assert.Null(PerformanceCoresDetector.BuildMask([]));
    }

    [Theory]
    [InlineData("0-3", new[] { 0, 1, 2, 3 })]
    [InlineData("0-3,8,10-11\n", new[] { 0, 1, 2, 3, 8, 10, 11 })]
    [InlineData("5", new[] { 5 })]
    [InlineData("", new int[0])]
    [InlineData("3-1", new int[0])]
    [InlineData("a-b", new int[0])]
    public void LinuxCpuListIsParsed(string cpuList, int[] expected)
    {
        Assert.Equal(expected, PerformanceCoresDetector.ParseCpuList(cpuList));
    }

    [Fact]
    public void WindowsProcessorCoresAreParsed()
    {
        // P-cores with hyper-threading (0-1, 2-3), an E-core (4) and a core in another processor group
        var buffer = CreateProcessorCore(efficiencyClass: 1, group: 0, mask: 0b11)
            .Concat(CreateProcessorCore(efficiencyClass: 1, group: 0, mask: 0b1100))
            .Concat(CreateProcessorCore(efficiencyClass: 0, group: 0, mask: 0b10000))
            .Concat(CreateProcessorCore(efficiencyClass: 1, group: 1, mask: 0b1))
            .ToArray();

        var processors = PerformanceCoresDetector.ParseWindowsProcessorCores(buffer, buffer.Length);

        Assert.Equal([(0, 1), (1, 1), (2, 1), (3, 1), (4, 0)], processors);
        Assert.Equal(0b1111UL, PerformanceCoresDetector.BuildMask(processors));
    }

    [Fact]
    public void WindowsProcessorCoresWithoutEfficiencyClassAreHomogeneous()
    {
        // before Windows 10 the efficiency class is always 0
        var buffer = CreateProcessorCore(efficiencyClass: 0, group: 0, mask: 0b11)
            .Concat(CreateProcessorCore(efficiencyClass: 0, group: 0, mask: 0b1100))
            .ToArray();

        Assert.Null(PerformanceCoresDetector.BuildMask(PerformanceCoresDetector.ParseWindowsProcessorCores(buffer, buffer.Length)));
    }

    [Fact]
    public void TruncatedWindowsBufferIsIgnored()
    {
        var buffer = CreateProcessorCore(efficiencyClass: 1, group: 0, mask: 0b1);

        Assert.Empty(PerformanceCoresDetector.ParseWindowsProcessorCores(buffer, buffer.Length - 1));
    }

    [Fact]
    public void PerformanceCoresAffinityIsAppliedToTheJobWhenDetected()
    {
        var job = Job.Dry.WithPerformanceCoresAffinity();

        if (PerformanceCoresDetector.TryGetPerformanceCoresAffinity(out var affinity))
            Assert.Equal(affinity, job.Environment.Affinity);
        else // the CPU has homogeneous cores or the OS is not supported
            Assert.False(job.HasValue(EnvironmentMode.AffinityCharacteristic));
    }

    /// <summary>
    /// Creates a SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX entry of type RelationProcessorCore with a single GROUP_AFFINITY.
    /// </summary>
    private static byte[] CreateProcessorCore(byte efficiencyClass, ushort group, ulong mask)
    {
        // Relationship (4) + Size (4) + PROCESSOR_RELATIONSHIP { Flags (1), EfficiencyClass (1), Reserved (20), GroupCount (2), GROUP_AFFINITY[] }
        // GROUP_AFFINITY { KAFFINITY Mask (pointer sized), WORD Group, WORD Reserved[3] }
        const int groupMaskOffset = 32;
        int size = groupMaskOffset + IntPtr.Size + 4 * sizeof(ushort);
        var entry = new byte[size];
        BitConverter.GetBytes(0).CopyTo(entry, 0); // RelationProcessorCore
        BitConverter.GetBytes(size).CopyTo(entry, 4);
        entry[9] = efficiencyClass;
        BitConverter.GetBytes((ushort) 1).CopyTo(entry, 30); // GroupCount
        if (IntPtr.Size == 8)
            BitConverter.GetBytes(mask).CopyTo(entry, groupMaskOffset);
        else
            BitConverter.GetBytes((uint) mask).CopyTo(entry, groupMaskOffset);
        BitConverter.GetBytes(group).CopyTo(entry, groupMaskOffset + IntPtr.Size);
        return entry;
    }
}
