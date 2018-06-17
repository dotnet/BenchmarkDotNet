namespace BenchmarkDotNet.Portability.Cpu
{
    public class CpuInfo
    {
        public string ProcessorName { get; }
        public int? PhysicalProcessorCount { get; }
        public int? PhysicalCoreCount { get; }
        public int? LogicalCoreCount { get; }
        public double? NominalFrequency { get; }

        public CpuInfo(string processorName, int? physicalProcessorCount, int? physicalCoreCount, int? logicalCoreCount, double? nominalFrequency)
        {
            ProcessorName = processorName;
            PhysicalProcessorCount = physicalProcessorCount;
            PhysicalCoreCount = physicalCoreCount;
            LogicalCoreCount = logicalCoreCount;
            NominalFrequency = nominalFrequency;
        }
    }
}