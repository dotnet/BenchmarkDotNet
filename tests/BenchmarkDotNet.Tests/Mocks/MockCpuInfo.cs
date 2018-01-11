using BenchmarkDotNet.Portability.Cpu;

namespace BenchmarkDotNet.Tests.Mocks
{
    public class MockCpuInfo : ICpuInfo
    {
        public int? PhysicalCoreCount { get; set; }

        public int? PhysicalProcessorCount { get; set; }

        public int? LogicalCoreCount { get; set; }

        public string ProcessorName { get; set; }
    }
}