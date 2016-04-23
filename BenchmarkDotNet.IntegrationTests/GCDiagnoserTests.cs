using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ListEnumeratorsBenchmarks
    {
        private List<int> list;

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

#if !CORE
    // this class is not compiled for CORE because it is using Diagnosers that currently do not support Core
    public class GCDiagnoserTests 
    {
        [Fact]
        public void Test()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(ListEnumeratorsBenchmarks));
            var gcDiagnoser = new Diagnostics.Windows.GCDiagnoser();

            var summary = BenchmarkRunner
                .Run(benchmarks, 
                    ManualConfig.CreateEmpty()
                        .With(Job.Dry.With(Runtime.Dnx).With(Jit.Host).With(Mode.Throughput).WithTargetCount(1))
                        .With(Job.Dry.With(Runtime.Core).With(Jit.Host).With(Mode.Throughput).WithTargetCount(1))
                        .With(gcDiagnoser));

            var gcCollectionColumns = gcDiagnoser.GetColumns.OfType<Diagnostics.Windows.GCDiagnoser.GCCollectionColumn>().ToArray();
            var listStructEnumeratorBenchmarks = benchmarks.Where(benchmark => benchmark.ShortInfo.Contains("ListStructEnumerator"));
            var listObjectEnumeratorBenchmarks = benchmarks.Where(benchmark => benchmark.ShortInfo.Contains("ListObjectEnumerator"));
            const int gen0Index = 0;

            foreach (var listStructEnumeratorBenchmark in listStructEnumeratorBenchmarks)
            {
                var structEnumeratorGen0Collections = gcCollectionColumns[gen0Index].GetValue(
                    summary,
                    listStructEnumeratorBenchmark);

                Assert.Equal("-", structEnumeratorGen0Collections);
            }

            foreach (var listObjectEnumeratorBenchmark in listObjectEnumeratorBenchmarks)
            {
                var objectEnumeratorGen0Collections = gcCollectionColumns[gen0Index].GetValue(
                    summary,
                    listObjectEnumeratorBenchmark);

                Assert.True(double.Parse(objectEnumeratorGen0Collections) > 0);
            }
        }
    }
#endif
}