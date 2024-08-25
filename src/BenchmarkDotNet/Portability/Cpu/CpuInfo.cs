using BenchmarkDotNet.Portability.Cpu.Linux;
using BenchmarkDotNet.Portability.Cpu.macOS;
using BenchmarkDotNet.Portability.Cpu.Windows;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Portability.Cpu;

public class CpuInfo(
    string? processorName,
    int? physicalProcessorCount,
    int? physicalCoreCount,
    int? logicalCoreCount,
    Frequency? nominalFrequency,
    Frequency? maxFrequency = null)
{
    public static readonly CpuInfo Empty = new (null, null, null, null, null, null);
    public static CpuInfo FromName(string processorName) => new (processorName, null, null, null, null);
    public static CpuInfo FromNameAndFrequency(string processorName, Frequency nominalFrequency) => new (processorName, null, null, null, nominalFrequency);

    private static readonly CompositeCpuInfoDetector Detector = new (
        new WindowsCpuInfoDetector(),
        new LinuxCpuInfoDetector(),
        new MacOsCpuInfoDetector());

    public static CpuInfo? DetectCurrent() => Detector.Detect();

    public string? ProcessorName { get; } = processorName;
    public int? PhysicalProcessorCount { get; } = physicalProcessorCount;
    public int? PhysicalCoreCount { get; } = physicalCoreCount;
    public int? LogicalCoreCount { get; } = logicalCoreCount;
    public Frequency? NominalFrequency { get; } = nominalFrequency ?? maxFrequency;
    public Frequency? MaxFrequency { get; } = maxFrequency ?? nominalFrequency;
}