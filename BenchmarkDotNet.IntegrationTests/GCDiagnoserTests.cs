#if !CORE
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(ThroughputFastConfig))]
    public class GCDiagnoserTests
    {
        private List<int> list;

        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var gcDiagnoser = new GCDiagnoser();
            var config = DefaultConfig.Instance.With(logger).With(gcDiagnoser);
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(GCDiagnoserTests), config);

            var summary = BenchmarkRunner.Run(benchmarks, config);

            var gcCollectionColumns = gcDiagnoser.GetColumns.OfType<GCDiagnoser.GCCollectionColumn>().ToArray();
            var listStructEnumeratorBenchmark = benchmarks.Single(benchmark => benchmark.ShortInfo.Contains("ListStructEnumerator"));
            var listObjectEnumeratorBenchmark = benchmarks.Single(benchmark => benchmark.ShortInfo.Contains("ListObjectEnumerator"));
            const int gen0Index = 0;
            var structEnumeratorGen0Collections = gcCollectionColumns[gen0Index].GetValue(summary, listStructEnumeratorBenchmark);
            var objectEnumeratorGen0Collections = gcCollectionColumns[gen0Index].GetValue(summary, listObjectEnumeratorBenchmark);

            Assert.Equal("-", structEnumeratorGen0Collections);
            Assert.True(double.Parse(objectEnumeratorGen0Collections) > 0);
        }

        [Setup]
        public void SetUp()
        {
            list = Enumerable.Range(0, 100).ToList();
        }

        [Benchmark]
        public int ListStructEnumerator()
        {
            int sum = 0;
            foreach (var i in list)
            {
                sum += i;
            }
            return sum;
        }

        [Benchmark]
        public int ListObjectEnumerator()
        {
            int sum = 0;
            foreach (var i in (IEnumerable<int>)list)
            {
                sum += i;
            }
            return sum;
        }
    }
}
#endif