using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    [DryJob]
    [RankColumn]
    public class IntroOrderManual
    {
        private class Config : ManualConfig
        {
            public Config() => Orderer = new FastestToSlowestOrderer();

            private class FastestToSlowestOrderer : IOrderer
            {
                public IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarksCase,
                    IEnumerable<BenchmarkLogicalGroupRule>? order = null) =>
                    from benchmark in benchmarksCase
                    orderby benchmark.Parameters["X"] descending,
                        benchmark.Descriptor.WorkloadMethodDisplayInfo
                    select benchmark;

                public IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCase, Summary summary) =>
                    from benchmark in benchmarksCase
                    orderby summary[benchmark].ResultStatistics.Mean
                    select benchmark;

                public string GetHighlightGroupKey(BenchmarkCase benchmarkCase) => null;

                public string GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase) =>
                    benchmarkCase.Job.DisplayInfo + "_" + benchmarkCase.Parameters.DisplayInfo;

                public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups,
                    IEnumerable<BenchmarkLogicalGroupRule>? order = null) =>
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