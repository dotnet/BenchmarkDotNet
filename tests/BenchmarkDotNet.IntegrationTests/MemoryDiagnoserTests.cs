using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(DiagnoserConfig))]
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
    public class MemoryDiagnoserTests
    {
        private readonly ITestOutputHelper output;

        public MemoryDiagnoserTests(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        [Fact(Skip = "Temporarily suppressed, see https://github.com/PerfDotNet/BenchmarkDotNet/issues/208")]
        public void MemoryDiagnoserTracksHeapMemoryAllocation()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(ListEnumeratorsBenchmarks));
            var memoryDiagnoser = new Diagnostics.Windows.MemoryDiagnoser();

            var summary = BenchmarkRunner
                .Run(benchmarks,
                    ManualConfig.CreateEmpty()
                        .With(Job.Dry.With(Runtime.Core).With(Jit.Host).With(Mode.Throughput).WithWarmupCount(1).WithTargetCount(1))
                        .With(DefaultConfig.Instance.GetLoggers().ToArray())
                        .With(DefaultConfig.Instance.GetColumns().ToArray())
                        .With(memoryDiagnoser)
                        .With(new OutputLogger(output)));

            var gcCollectionColumns = memoryDiagnoser.GetColumns.OfType<Diagnostics.Windows.MemoryDiagnoser.GCCollectionColumn>().ToArray();
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
                var gen0Str = gcCollectionColumns[gen0Index].GetValue(
                    summary,
                    listObjectEnumeratorBenchmark);

                double gen0Value;
                if (double.TryParse(gen0Str, NumberStyles.Number, HostEnvironmentInfo.MainCultureInfo, out gen0Value))
                    Assert.True(gen0Value > 0);
                else
                {
                    Assert.True(false, $"Can't parse '{gen0Str}'");
                }
            }
        }
    }
#endif
}