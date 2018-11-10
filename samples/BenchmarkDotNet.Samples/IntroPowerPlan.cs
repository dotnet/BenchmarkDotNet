using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [HighPerformancePowerPlan(false)]
    public class IntroPowerPlan
    {
        [Benchmark]
        public int SplitJoin()
            => string.Join(",", new string[1000]).Split(',').Length;
    }
}
