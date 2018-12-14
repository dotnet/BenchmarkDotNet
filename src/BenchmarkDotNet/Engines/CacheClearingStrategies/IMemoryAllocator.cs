namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal interface IMemoryAllocator
    {
        void AllocateMemory();
    }
}