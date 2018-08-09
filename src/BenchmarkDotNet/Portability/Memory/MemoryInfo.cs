namespace BenchmarkDotNet.Portability.Memory
{
    public class MemoryInfo
    {
        public MemoryInfo(long totalMemory, long freePhysicalMemory)
        {
            TotalMemory = totalMemory;
            FreePhysicalMemory = freePhysicalMemory;
        }

        public long TotalMemory { get; set; }
        public long FreePhysicalMemory { get; set; }
    }
}