using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
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

            var groupped = benchmarks1.BenchmarksCases.Union(benchmarks2.BenchmarksCases)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Single(groupped); // we should have single exe!
            Assert.Equal(benchmarks1.BenchmarksCases.Length + benchmarks2.BenchmarksCases.Length, groupped.Single().Count());
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

            var groupped = benchmarks.BenchmarksCases
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

        [Fact]
        public void CustomClrBuildJobsAreGrouppedByVersion()
        {
            const string version = "abcd";

            var config = ManualConfig.Create(DefaultConfig.Instance)
                .With(Job.Default.With(new ClrRuntime(version: version)))
                .With(Job.Default.With(new ClrRuntime(version: "it's a different version")))
                .With(Job.Clr);

            var benchmarks1 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain1), config);
            var benchmarks2 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain2), config);

            var groupped = benchmarks1.BenchmarksCases.Union(benchmarks2.BenchmarksCases)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Equal(3, groupped.Length); // Job.Clr + Job.Clr(version) + Job.Clr(different)

            foreach (var grouping in groupped)
                Assert.Equal(3 * 2, grouping.Count()); // (M1 + M2 + M3) * (Plain1 + Plain2)
        }
    }
}