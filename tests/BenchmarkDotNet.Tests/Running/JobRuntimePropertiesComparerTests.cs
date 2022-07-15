using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.CsProj;
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

        public class Plain3
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

        [SimpleJob(runtimeMoniker: RuntimeMoniker.Net462)]
        [SimpleJob(runtimeMoniker: RuntimeMoniker.Mono)]
        [SimpleJob(runtimeMoniker: RuntimeMoniker.Net50)]
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
                .AddJob(Job.Default.WithRuntime(ClrRuntime.CreateForLocalFullNetFrameworkBuild(version: version)))
                .AddJob(Job.Default.WithRuntime(ClrRuntime.CreateForLocalFullNetFrameworkBuild(version: "it's a different version")))
                .AddJob(Job.Default.WithRuntime(ClrRuntime.GetCurrentVersion()));

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
                .AddJob(job1)
                .AddJob(job2);

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
                .AddJob(Job.Default.WithNuGet("AutoMapper", "7.0.1"))
                .AddJob(Job.Default.WithNuGet("AutoMapper", "7.0.0-alpha-0001"));

            var benchmarks1 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain1), config);
            var benchmarks2 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain2), config);

            var grouped = benchmarks1.BenchmarksCases.Union(benchmarks2.BenchmarksCases)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Equal(2, grouped.Length); // 7.0.1 + 7.0.0-alpha-0001

            foreach (var grouping in grouped)
                Assert.Equal(3 * 2, grouping.Count()); // (M1 + M2 + M3) * (Plain1 + Plain2)
        }

        [Fact]
        public void CustomTargetPlatformJobsAreGroupedByTargetFrameworkMoniker()
        {
            var net5Config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp50));
            var net5WindowsConfig1 = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.From(new Toolchains.DotNetCli.NetCoreAppSettings(
                    targetFrameworkMoniker: "net5.0-windows",
                    runtimeFrameworkVersion: null,
                    name: ".NET 5.0"))));
            // a different INSTANCE of CsProjCoreToolchain that also targets "net5.0-windows"
            var net5WindowsConfig2 = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.From(new Toolchains.DotNetCli.NetCoreAppSettings(
                    targetFrameworkMoniker: "net5.0-windows",
                    runtimeFrameworkVersion: null,
                    name: ".NET 5.0"))));

            var benchmarksNet5 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain1), net5Config);
            var benchmarksNet5Windows1 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain2), net5WindowsConfig1);
            var benchmarksNet5Windows2 = BenchmarkConverter.TypeToBenchmarks(typeof(Plain3), net5WindowsConfig2);

            var grouped = benchmarksNet5.BenchmarksCases
                .Union(benchmarksNet5Windows1.BenchmarksCases)
                .Union(benchmarksNet5Windows2.BenchmarksCases)
                .GroupBy(benchmark => benchmark, new BenchmarkPartitioner.BenchmarkRuntimePropertiesComparer())
                .ToArray();

            Assert.Equal(2, grouped.Length);

            Assert.Single(grouped, group => group.Count() == 3); // Plain1 (3 methods) runing against "net5.0"
            Assert.Single(grouped, group => group.Count() == 6); // Plain2 (3 methods) and Plain3 (3 methods) runing against "net5.0-windows"
        }
    }
}