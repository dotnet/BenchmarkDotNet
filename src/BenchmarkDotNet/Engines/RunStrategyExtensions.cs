namespace BenchmarkDotNet.Engines
{
    public static class RunStrategyExtensions
    {
        public static bool NeedsJitting(this RunStrategy runStrategy) => runStrategy == RunStrategy.Throughput;
    }
}