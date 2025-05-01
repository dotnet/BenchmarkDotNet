using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu;

/// <summary>
/// Loads the <see cref="CpuInfo"/> for the current hardware
/// </summary>
public interface ICpuDetector
{
    bool IsApplicable();
    CpuInfo? Detect();
}