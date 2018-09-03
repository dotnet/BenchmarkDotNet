namespace BenchmarkDotNet.Engines
{
    public enum IterationMode : int
    {
        Overhead,
        
        Workload,
        
        Unknown
    }
}