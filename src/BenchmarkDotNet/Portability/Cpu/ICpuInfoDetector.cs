namespace BenchmarkDotNet.Portability.Cpu;

/// <summary>
/// Loads the <see cref="CpuInfo"/> for the current hardware
/// </summary>
internal interface ICpuInfoDetector
{
    bool IsApplicable();
    CpuInfo? Detect();
}