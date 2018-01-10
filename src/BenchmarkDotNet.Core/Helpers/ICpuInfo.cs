namespace BenchmarkDotNet.Helpers
{
    public interface ICpuInfo
    {
        int? PhysicalCoreCount { get; }

        int? PhysicalProcessorCount { get; }
        
        int? LogicalCoreCount { get; }

        string ProcessorName { get; }
    }
}