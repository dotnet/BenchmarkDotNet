using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    [DryJob]
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

                public string GetHighlightGroupKey(Benchmark benchmark) => null;

                public string GetLogicalGroupKey(IConfig config, Benchmark[] allBenchmarks, Benchmark benchmark) =>
                    benchmark.Job.DisplayInfo + "_" + benchmark.Parameters.DisplayInfo;

                public IEnumerable<IGrouping<string, Benchmark>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, Benchmark>> logicalGroups) => 
                    logicalGroups.OrderBy(it => it.Key);

                public bool SeparateLogicalGroups => true;
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