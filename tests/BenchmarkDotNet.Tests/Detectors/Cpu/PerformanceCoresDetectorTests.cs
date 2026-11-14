using BenchmarkDotNet.Detectors.Cpu;

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
    public void WindowsCpuSetsAreParsed()
    {
        // (group, logical processor index, efficiency class)
        (ushort, byte, byte)[] cpuSets = [(0, 0, 1), (0, 1, 1), (0, 2, 0), (1, 0, 1)];
        var buffer = cpuSets.SelectMany(cpuSet => CreateCpuSet(cpuSet.Item1, cpuSet.Item2, cpuSet.Item3)).ToArray();

        var processors = PerformanceCoresDetector.ParseWindowsCpuSets(buffer, buffer.Length);

        Assert.Equal([(0, 1), (1, 1), (2, 0)], processors);
        Assert.Equal(0b11UL, PerformanceCoresDetector.BuildMask(processors));
    }

    private static byte[] CreateCpuSet(ushort group, byte logicalProcessorIndex, byte efficiencyClass)
    {
        const int size = 32; // sizeof(SYSTEM_CPU_SET_INFORMATION)
        var entry = new byte[size];
        BitConverter.GetBytes(size).CopyTo(entry, 0);
        BitConverter.GetBytes(0).CopyTo(entry, 4); // CpuSetInformation
        BitConverter.GetBytes(256 + logicalProcessorIndex).CopyTo(entry, 8); // Id
        BitConverter.GetBytes(group).CopyTo(entry, 12);
        entry[14] = logicalProcessorIndex;
        entry[18] = efficiencyClass;
        return entry;
    }
}
