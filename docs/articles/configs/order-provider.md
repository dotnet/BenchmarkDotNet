# OrderProvider

OrderProviders allows customizing the order of benchmark results in the summary table.

A usage example:

```cs
[Config(typeof(Config))]
[RankColumn]
public class IntroOrderManual
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Set(new FastestToSlowestOrderProvider());
        }

        private class FastestToSlowestOrderProvider : IOrderProvider
        {
            public IEnumerable<Benchmark> GetExecutionOrder(Benchmark[] benchmarks) =>
                from benchmark in benchmarks
                orderby benchmark.Parameters["X"] descending,
                        benchmark.Target.MethodDisplayInfo
                select benchmark;

            public IEnumerable<Benchmark> GetSummaryOrder(Benchmark[] benchmarks, Summary summary) =>
                from benchmark in benchmarks
                orderby summary[benchmark].ResultStatistics.Mean
                select benchmark;

            public string GetGroupKey(Benchmark benchmark, Summary summary) => null;
        }
    }

    [Params(1, 2, 3)]
    public int X { get; set; }

    [Benchmark]
    public void Fast() => Thread.Sleep(X * 50);

    [Benchmark]
    public void Slow() => Thread.Sleep(X * 100);
}
```