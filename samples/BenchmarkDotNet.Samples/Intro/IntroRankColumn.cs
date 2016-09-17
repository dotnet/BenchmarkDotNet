using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Samples.Intro
{
    [ShortRunJob]
    [OrderProvider(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(RankColumn.Kind.Arabic)]
    [RankColumn(RankColumn.Kind.Roman)]
    [RankColumn(RankColumn.Kind.Stars)]
    [RankColumn(RankColumn.Kind.Words)]
    public class IntroRankColumn
    {
        [Params(1, 2)]
        public int Factor;

        [Benchmark]
        public void Foo() => Thread.Sleep(Factor * 100);

        [Benchmark]
        public void Bar() => Thread.Sleep(Factor * 200);
    }
}