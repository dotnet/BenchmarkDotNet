using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests
{
    public class JobIdFilterTests
    {
        private static readonly IConfig ConfigWithNamedJobs = ManualConfig.CreateEmpty()
            .AddJob(Job.Dry.WithId("net8"))
            .AddJob(Job.Dry.WithId("net9"))
            .AddJob(Job.Dry.WithId("debug"));

        [Theory]
        [InlineData("net8", 1)]
        [InlineData("NET8", 1)] // case insensitive
        [InlineData("net9", 1)]
        [InlineData("debug", 1)]
        [InlineData("net*", 2)] // glob
        [InlineData("*", 3)]
        [InlineData("net", 0)] // it's an exact match, not a substring one
        [InlineData("WRONG", 0)]
        public void TheFilterSelectsBenchmarksByJobId(string pattern, int expectedBenchmarks)
        {
            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithSingleBenchmark), ConfigWithNamedJobs).BenchmarksCases;
            Assert.Equal(3, benchmarkCases.Length); // one per job

            var filter = new JobIdFilter([pattern]);

            Assert.Equal(expectedBenchmarks, benchmarkCases.Count(benchmarkCase => filter.Predicate(benchmarkCase)));
        }

        [Fact]
        public void MultiplePatternsAreCombinedWithOr()
        {
            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithSingleBenchmark), ConfigWithNamedJobs).BenchmarksCases;

            var filter = new JobIdFilter(["net8", "net9"]);

            var matched = benchmarkCases.Where(benchmarkCase => filter.Predicate(benchmarkCase)).ToArray();

            Assert.Equal(2, matched.Length);
            Assert.Equal(["net8", "net9"], matched.Select(benchmarkCase => benchmarkCase.Job.ResolvedId).OrderBy(id => id));
        }

        [Fact]
        public void JobsDefinedViaAttributesCanBeSelectedToo()
        {
            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithNamedJobAttributes)).BenchmarksCases;

            var filter = new JobIdFilter(["fromAttribute"]);

            var matched = Assert.Single(benchmarkCases, benchmarkCase => filter.Predicate(benchmarkCase));
            Assert.Equal("fromAttribute", matched.Job.ResolvedId);
        }

        [Fact]
        public void JobsWithNoExplicitIdAreMatchedByTheirGeneratedId()
        {
            var benchmarkCase = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithSingleBenchmark)).BenchmarksCases.Single();
            Assert.Equal("DefaultJob", benchmarkCase.Job.ResolvedId);

            Assert.True(new JobIdFilter(["DefaultJob"]).Predicate(benchmarkCase));
            Assert.False(new JobIdFilter(["net8"]).Predicate(benchmarkCase));
        }

        [Fact]
        public void TheFilterRecordsWhatItHasObservedAndMatched()
        {
            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithSingleBenchmark), ConfigWithNamedJobs).BenchmarksCases;

            var filter = new JobIdFilter(["net8"]);
            foreach (var benchmarkCase in benchmarkCases)
                filter.Predicate(benchmarkCase);

            Assert.Equal(["debug", "net8", "net9"], filter.ObservedJobIds.OrderBy(id => id));
            Assert.Equal(["net8"], filter.MatchedJobIds);
        }

        [Fact]
        public void TheFilterRecordsNoMatchWhenNoJobIdMatches()
        {
            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithSingleBenchmark), ConfigWithNamedJobs).BenchmarksCases;

            var filter = new JobIdFilter(["typo"]);
            foreach (var benchmarkCase in benchmarkCases)
                filter.Predicate(benchmarkCase);

            Assert.NotEmpty(filter.ObservedJobIds);
            Assert.Empty(filter.MatchedJobIds);
        }
    }

    public class TypeWithSingleBenchmark
    {
        [Benchmark] public void TheBenchmark() { }
    }

    [SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 1, iterationCount: 1, id: "fromAttribute")]
    public class TypeWithNamedJobAttributes
    {
        [Benchmark] public void TheBenchmark() { }
    }
}
