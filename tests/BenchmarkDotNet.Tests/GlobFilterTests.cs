using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class GlobFilterTests
    {
        [Theory]
        [InlineData(nameof(TypeWithBenchmarks), true)] // type name
        [InlineData("typewithbenchmarks", true)] // type name lowercase
        [InlineData("TYPEWITHBENCHMARKS", true)] // type name uppercase
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

        public class TypeWithBenchmarks
        {
            [Benchmark] public void TheBenchmark() { }
        }
    }
}