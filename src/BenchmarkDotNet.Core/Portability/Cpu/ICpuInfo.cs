namespace BenchmarkDotNet.Portability.Cpu
{
    public interface ICpuInfo
    {
        int? PhysicalCoreCount { get; }

        int? PhysicalProcessorCount { get; }
        
        int? LogicalCoreCount { get; }

        string ProcessorName { get; }
    }
}