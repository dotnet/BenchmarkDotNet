using AwesomeAssertions;
using BenchmarkDotNet.Detectors;
using Perfolizer.Helpers;
using Perfolizer.Models;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

public class CpuDetectorTests(ITestOutputHelper Output)
{
    [Fact]
    public void DetectCpuInfo()
    {
        // Act
        CpuInfo? cpuInfo = CpuDetector.Cpu;

        // Assert
        cpuInfo.Should().NotBeNull();
        Output.WriteLine(cpuInfo.ToFullBrandName());
        if (cpuInfo.MaxFrequencyHz == null || cpuInfo.NominalFrequencyHz == null)
            return;

        cpuInfo.MaxFrequencyHz.Should().BeGreaterThanOrEqualTo(cpuInfo.NominalFrequencyHz.Value);

        Output.WriteLine($"MaxFrequencyHz: {cpuInfo.MaxFrequencyHz}");
        Output.WriteLine($"NominalFrequencyHz: {cpuInfo.NominalFrequencyHz}");
    }
}
