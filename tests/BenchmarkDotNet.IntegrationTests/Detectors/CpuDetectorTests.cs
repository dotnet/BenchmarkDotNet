using AwesomeAssertions;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Detectors.Cpu.Windows;
using BenchmarkDotNet.Tests.XUnit;
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
        cpuInfo.ToFullBrandName().Should().NotBe("Unknown processor");

        Output.WriteLine(cpuInfo.ToFullBrandName());
        if (cpuInfo.MaxFrequencyHz == null || cpuInfo.NominalFrequencyHz == null)
            return;

        Output.WriteLine($"MaxFrequencyHz: {cpuInfo.MaxFrequencyHz}");
        Output.WriteLine($"NominalFrequencyHz: {cpuInfo.NominalFrequencyHz}");

        // On Windows, CPU frequency that are returned by CIM/registory value has slightly different.
        // https://github.com/dotnet/BenchmarkDotNet/issues/859#issuecomment-4414842406
        // On Linux, There is issue wrong CPU frequency is returned on some CPU.
        // https://github.com/dotnet/BenchmarkDotNet/pull/3131#issuecomment-4455965694
        if (!OsDetector.IsWindows() && !OsDetector.IsLinux())
        {
            cpuInfo.MaxFrequencyHz.Should().BeGreaterThanOrEqualTo(cpuInfo.NominalFrequencyHz.Value);
        }
    }

    [FactEnvSpecific(EnvRequirement.WindowsOnly)]
    public void DetectCpuInfoOnWindowsAndCompareValues()
    {
        if (!OsDetector.IsWindows7OrLater())
            return;

        // Act
        CpuInfo? cpuInfo1 = new DefaultCpuDetector().Detect();
        CpuInfo? cpuInfo2 = new PowershellWmiCpuDetector().Detect();

        // Assert
        cpuInfo1.Should().NotBeNull();
        cpuInfo2.Should().NotBeNull();

        cpuInfo1.Should().BeEquivalentTo(cpuInfo2, options => options
            .Using<long?>(ctx =>
            {
                if (ctx.Expectation is null)
                {
                    ctx.Subject.Should().BeNull();
                    return;
                }

                var expected = (double)ctx.Expectation!.Value;
                var actual = (double)ctx.Subject!.Value;
                var tolerance = Math.Abs(expected) * 0.01d; // Accept 1％ difference
                actual.Should().BeApproximately(expected, tolerance);
            }).WhenTypeIs<long?>());
    }
}
