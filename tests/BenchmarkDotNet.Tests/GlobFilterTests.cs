using System.Linq;
using System.Threading;
using ApprovalUtilities.Utilities;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class GlobFilterTests
    {
        [Theory]
        [InlineData(nameof(TypeWithBenchmarks), false)] // type name
        [InlineData("typewithbenchmarks", false)] // type name lowercase
        [InlineData("TYPEWITHBENCHMARKS", false)] // type name uppercase
        [InlineData("*TypeWithBenchmarks*", true)] // regular expression
        [InlineData("*typewithbenchmarks*", true)] // regular expression lowercase
        [InlineData("*TYPEWITHBENCHMARKS*", true)] // regular expression uppercase
        [InlineData("*", true)]
        [InlineData("WRONG", false)]
        [InlineData("*stillWRONG*", false)]
        public void TheFilterIsCaseInsensitive(string pattern, bool expected)
        {
            var benchmarkCase  = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithBenchmarks)).BenchmarksCases.Single();

            var filter = new GlobFilter(new[] { pattern });

            Assert.Equal(expected, filter.Predicate(benchmarkCase));
        }

        [Theory]
        [InlineData(nameof(TypeWithBenchmarksAndParams), 0)] // type name
        [InlineData("typewithbenchmarksandparams", 0)] // type name lowercase
        [InlineData("TYPEWITHBENCHMARKSANDPARAMS", 0)] // type name uppercase
        [InlineData("*TypeWithBenchmarksAndParams*", 2)] // regular expression
        [InlineData("*typewithbenchmarksandparams*", 2)] // regular expression lowercase
        [InlineData("*TYPEWITHBENCHMARKSANDPARAMS*", 2)] // regular expression uppercase
        [InlineData("*", 2)]
        [InlineData("WRONG", 0)]
        [InlineData("*stillWRONG*", 0)]
        [InlineData("BenchmarkDotNet.Tests.TypeWithBenchmarksAndParams.TheBenchmark", 2)]
        [InlineData("BenchmarkDotNet.Tests.TypeWithBenchmarksAndParams.TheBenchmark(A: 100)", 1)]
        public void TheFilterWorksWithParams(string pattern, int expectedBenchmarks)
        {
            var benchmarkCases = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithBenchmarksAndParams)).BenchmarksCases;

            var filter = new GlobFilter(new[] { pattern });

            Assert.Equal(expectedBenchmarks, benchmarkCases.Where(benchmarkCase => filter.Predicate(benchmarkCase)).Count());
        }
    }

    public class TypeWithBenchmarks
    {
        [Benchmark] public void TheBenchmark() { }
    }

    public class TypeWithBenchmarksAndParams
    {
        [Params(100, 200)]
        public int A { get; set; }

        [Benchmark] public void TheBenchmark() { }
    }
}