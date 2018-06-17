namespace BenchmarkDotNet.Portability.Cpu
{
    public class CpuInfo
    {
        public string ProcessorName { get; }
        public int? PhysicalProcessorCount { get; }
        public int? PhysicalCoreCount { get; }
        public int? LogicalCoreCount { get; }
        public double? NominalFrequency { get; }
        public double? MinFrequency { get; }
        public double? MaxFrequency { get; }
        public double? EffectiveFrequency { get; }
        

        public CpuInfo(string processorName,
                       int? physicalProcessorCount,
                       int? physicalCoreCount,
                       int? logicalCoreCount,
                       double? nominalFrequency,
                       double? minFrequency,
                       double? maxFrequency,
                       double? effectiveFrequency = null)
        {
            ProcessorName = processorName;
            PhysicalProcessorCount = physicalProcessorCount;
            PhysicalCoreCount = physicalCoreCount;
            LogicalCoreCount = logicalCoreCount;
            NominalFrequency = nominalFrequency;
            MinFrequency = minFrequency;
            MaxFrequency = maxFrequency;
            EffectiveFrequency = effectiveFrequency;
        }
    }
}