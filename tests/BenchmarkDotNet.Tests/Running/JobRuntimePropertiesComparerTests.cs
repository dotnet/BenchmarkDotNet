using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;

namespace BenchmarkDotNet.Tests.Running
{
    public class JobRuntimePropertiesComparerTests
    {
        [Fact]
        public void SingleJobLeadsToNoGrouping()
        {
            var benchmarks1 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain1));
            var benchmarks2 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain2));

            var grouped = benchmarks1.BenchmarksCases.Union(benchmarks2.BenchmarksCases)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Single(grouped); // we should have single exe!
            Assert.Equal(benchmarks1.BenchmarksCases.Length + benchmarks2.BenchmarksCases.Length, grouped.Single().Count());
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
        public void BenchmarksAreGroupedByJob()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(AllRuntimes));

            var grouped = benchmarks.BenchmarksCases
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Equal(3, grouped.Length); // Clr + Mono + Core

            foreach (var grouping in grouped)
                Assert.Equal(2, grouping.Count()); // M1 + M2
        }

        [TargetFrameworkJob(TargetFrameworkMoniker.Net461)]
        [TargetFrameworkJob(TargetFrameworkMoniker.Mono)]
        [TargetFrameworkJob(TargetFrameworkMoniker.NetCoreApp21)]
        public class AllRuntimes
        {
            [Benchmark] public void M1() { }
            [Benchmark] public void M2() { }
        }

        [FactWindowsOnly("Full Framework is supported only on Windows")]
        public void CustomClrBuildJobsAreGroupedByVersion()
        {
            const string version = "abcd";

            var config = ManualConfig.Create(DefaultConfig.Instance)
                .With(Job.Default.With(ClrRuntime.CreateForLocalFullNetFrameworkBuild(version: version)))
                .With(Job.Default.With(ClrRuntime.CreateForLocalFullNetFrameworkBuild(version: "it's a different version")))
                .With(Job.Default.With(ClrRuntime.GetCurrentVersion()));

            var benchmarks1 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain1), config);
            var benchmarks2 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain2), config);

            var grouped = benchmarks1.BenchmarksCases.Union(benchmarks2.BenchmarksCases)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Equal(3, grouped.Length); // Job.Clr + Job.Clr(version) + Job.Clr(different)

            foreach (var grouping in grouped)
                Assert.Equal(3 * 2, grouping.Count()); // (M1 + M2 + M3) * (Plain1 + Plain2)
        }

        [Fact]
        public void CustomNuGetJobsWithSamePackageVersionAreGroupedTogether()
        {
            var job1 = Job.Default.WithNuGet("AutoMapper", "7.0.1");
            var job2 = Job.Default.WithNuGet("AutoMapper", "7.0.1");

            var config = ManualConfig.Create(DefaultConfig.Instance)
                .With(job1)
                .With(job2);
            
            var benchmarks1 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain1), config);
            var benchmarks2 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain2), config);

            var grouped = benchmarks1.BenchmarksCases.Union(benchmarks2.BenchmarksCases)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Single(grouped); // 7.0.1

            foreach (var grouping in grouped)
                Assert.Equal(2 * 3 * 2, grouping.Count()); // ((job1 + job2) * (M1 + M2 + M3) * (Plain1 + Plain2)
        }

        [Fact]
        public void CustomNuGetJobsAreGroupedByPackageVersion()
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .With(Job.Default.WithNuGet("AutoMapper", "7.0.1"))
                .With(Job.Default.WithNuGet("AutoMapper", "7.0.0-alpha-0001"));

            var benchmarks1 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain1), config);
            var benchmarks2 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain2), config);

            var grouped = benchmarks1.BenchmarksCases.Union(benchmarks2.BenchmarksCases)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Equal(2, grouped.Length); // 7.0.1 + 7.0.0-alpha-0001

            foreach (var grouping in grouped)
                Assert.Equal(3 * 2, grouping.Count()); // (M1 + M2 + M3) * (Plain1 + Plain2)
        }
    }
}