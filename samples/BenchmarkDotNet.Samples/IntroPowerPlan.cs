using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [HighPerformancePowerPlan(false)]
    public class IntroPowerPlanDisabled
    {
        [Benchmark]
        public int SplitJoin()
            => string.Join(",", new string[1000]).Split(',').Length;
    }

    // By default benchmark.net uses high-performance power plan.
    // There is no need to set it to true explicitly
    [HighPerformancePowerPlan(true)]
    public class IntroPowerPlanEnabled
    {
        [Benchmark]
        public int SplitJoin()
            => string.Join(",", new string[1000]).Split(',').Length;
    }
}
