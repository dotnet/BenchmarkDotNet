using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Tests.Environments;

public class PhysicalMemoryInfoTests
{
    [Fact]
    public void MacAvailableMemoryIncludesReusablePages()
    {
        const string vmStat = """
            Mach Virtual Memory Statistics: (page size of 16384 bytes)
            Pages free:                               100.
            Pages active:                           2000.
            Pages inactive:                          300.
            Pages speculative:                        50.
            Pages throttled:                           0.
            Pages wired down:                       500.
            Pages purgeable:                          25.
            """;

        SystemMemory.ParseMacAvailableMemory(vmStat).Should().Be(7_782_400);
    }

    [Fact]
    public void MacAvailableMemoryReturnsNullWhenNoPagesAreReported()
    {
        SystemMemory.ParseMacAvailableMemory("Mach Virtual Memory Statistics: (page size of 4096 bytes)")
            .Should().BeNull();
    }
}
