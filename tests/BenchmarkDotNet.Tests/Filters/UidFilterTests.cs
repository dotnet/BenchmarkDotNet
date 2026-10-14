using AwesomeAssertions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests.Filters;

public class UidFilterTests
{
    [Fact]
    public void FilterByUid()
    {
        // Arrange
        var benchmarkCase = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithBenchmarks)).BenchmarksCases.Single();
        var uid = benchmarkCase.GetUniqueId();
        var filter = new UidFilter([uid]);

        // Assert
        bool result = filter.Predicate(benchmarkCase);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FilterByUid_UpperCase()
    {
        // Arrange
        var benchmarkCase = BenchmarkConverter.TypeToBenchmarks(typeof(TypeWithBenchmarks)).BenchmarksCases.Single();
        var uid = benchmarkCase.GetUniqueId().ToUpperInvariant();
        var filter = new UidFilter([uid]);

        // Assert
        bool result = filter.Predicate(benchmarkCase);

        // Assert
        result.Should().BeTrue();
    }
}
