namespace BenchmarkDotNet.Engines
{
    public enum CacheClearingStrategy
    {
        None,
        Allocations,
        Native
    }
}