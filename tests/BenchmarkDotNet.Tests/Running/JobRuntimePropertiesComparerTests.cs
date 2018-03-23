using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests.Running
{
    public class JobRuntimePropertiesComparerTests
    {
        [Fact]
        public void SingleJobLeadsToNoGroupping()
        {
            var benchmarks1 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain1));
            var benchmarks2 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain2));

            var groupped = benchmarks1.Benchmarks.Union(benchmarks2.Benchmarks)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Single(groupped); // we should have single exe!
            Assert.Equal(benchmarks1.Benchmarks.Length + benchmarks2.Benchmarks.Length, groupped.Single().Count());
        }

        public class Plain1
        {
            [Benchmark] public void M1() { }
            [Benchmark] public void M2() { }
            [Benchmark] public void M3() { }
        }

        public class Plain2
        {
            [Benchmark] public void M1() { }
            [Benchmark] public void M2() { }
            [Benchmark] public void M3() { }
        }

        [Fact]
        public void BenchmarksAreGrouppedByJob()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(AllRuntimes));

            var groupped = benchmarks.Benchmarks
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Equal(3, groupped.Length); // Clr + Mono + Core

            foreach (var grouping in groupped)
                Assert.Equal(2, grouping.Count()); // M1 + M2
        }

        [ClrJob, MonoJob, CoreJob]
        public class AllRuntimes
        {
            [Benchmark] public void M1() { }
            [Benchmark] public void M2() { }
        }
    }
}