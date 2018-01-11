namespace BenchmarkDotNet.Portability.Cpu
{
    public interface ICpuInfo
    {
        string ProcessorName { get; }
        int? PhysicalProcessorCount { get; }
        int? PhysicalCoreCount { get; }
        int? LogicalCoreCount { get; }
    }
}