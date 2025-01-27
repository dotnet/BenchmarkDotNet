using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu;

/// <summary>
/// Loads the <see cref="PhdCpu"/> for the current hardware
/// </summary>
public interface ICpuDetector
{
    bool IsApplicable();
    PhdCpu? Detect();
}