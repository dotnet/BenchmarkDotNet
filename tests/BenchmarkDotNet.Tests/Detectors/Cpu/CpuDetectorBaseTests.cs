using AwesomeAssertions;
using BenchmarkDotNet.Detectors.Cpu;
using Perfolizer.Models;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

public class CpuDetectorBaseTests
{
    [Fact]
    public void ApplySmbiosNameAndFrequency_ShouldOnlyOverrideNameAndFrequencies()
    {
        // Arrange
        var parsed = new CpuInfo
        {
            ProcessorName = "Parser CPU",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 8,
            LogicalCoreCount = 16,
            NominalFrequencyHz = 2_500_000_000L,
            MaxFrequencyHz = 3_000_000_000L,
        };

        var smbios = new CpuInfo
        {
            ProcessorName = "SMBIOS CPU",
            PhysicalProcessorCount = 2,
            PhysicalCoreCount = 32,
            LogicalCoreCount = 64,
            NominalFrequencyHz = 3_600_000_000L,
            MaxFrequencyHz = 4_100_000_000L,
        };

        // Act
        var result = CpuDetectorBase.ApplySmbiosNameAndFrequency(parsed, smbios);

        // Assert
        result.ProcessorName.Should().Be("SMBIOS CPU");
        result.MaxFrequencyHz.Should().Be(4_100_000_000L);
        result.NominalFrequencyHz.Should().Be(3_600_000_000L);

        // Counts come from the parser, not from SMBIOS.
        result.PhysicalProcessorCount.Should().Be(1);
        result.PhysicalCoreCount.Should().Be(8);
        result.LogicalCoreCount.Should().Be(16);
    }

    [Fact]
    public void ApplySmbiosNameAndFrequency_ShouldKeepParserValueWhenSmbiosIsUnknown()
    {
        // Arrange
        var parsed = new CpuInfo
        {
            ProcessorName = "Parser CPU",
            MaxFrequencyHz = 3_000_000_000L,
        };

        var smbios = new CpuInfo();

        // Act
        var result = CpuDetectorBase.ApplySmbiosNameAndFrequency(parsed, smbios);

        // Assert
        result.ProcessorName.Should().Be("Parser CPU");
        result.MaxFrequencyHz.Should().Be(3_000_000_000L);
    }
}
