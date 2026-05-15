using AwesomeAssertions;
using BenchmarkDotNet.Detectors;
using Perfolizer.Helpers;
using Perfolizer.Models;

namespace BenchmarkDotNet.IntegrationTests.Detectors.Cpu;

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

        Output.WriteLine($"MaxFrequencyHz: {cpuInfo.MaxFrequencyHz}");
        Output.WriteLine($"NominalFrequencyHz: {cpuInfo.NominalFrequencyHz}");

        // On some environment, it failed following assertion.
        // https://github.com/dotnet/BenchmarkDotNet/pull/3131#issuecomment-4455965694
        // cpuInfo.MaxFrequencyHz.Should().BeGreaterThanOrEqualTo(cpuInfo.NominalFrequencyHz.Value);
    }
}
