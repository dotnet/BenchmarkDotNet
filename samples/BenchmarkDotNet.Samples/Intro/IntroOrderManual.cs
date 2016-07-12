using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    public class IntroOrderManual
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Dry);
                Set(new FastestToSlowestOrderProvider());
                Add(PlaceColumn.ArabicNumber);
            }

            private class FastestToSlowestOrderProvider : IOrderProvider
            {
                public IEnumerable<Benchmark> GetExecutionOrder(Benchmark[] benchmarks) =>
                    from benchmark in benchmarks
                    orderby benchmark.Parameters["X"] descending,
                            benchmark.Target.MethodTitle
                    select benchmark;

                public IEnumerable<Benchmark> GetSummaryOrder(Benchmark[] benchmarks, Summary summary) =>
                    from benchmark in benchmarks
                    orderby summary[benchmark].ResultStatistics.Median
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
}