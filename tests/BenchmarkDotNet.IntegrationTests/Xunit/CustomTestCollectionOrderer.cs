using Xunit.Sdk;
using Xunit.v3;

namespace BenchmarkDotNet.IntegrationTests.Xunit;

/// <summary>
/// Custom TestCollection orderer implementation.
/// Run the test collection marked `FlakyTests` with priority.
/// </summary>
internal sealed class CustomTestCollectionOrderer : ITestCollectionOrderer
{
    /// <inheritdoc/>
    public IReadOnlyCollection<T> OrderTestCollections<T>(IReadOnlyCollection<T> testCollections)
        where T : ITestCollection
    {
        var result = testCollections.ToList();
        result.Sort(Compare);
        return result;
    }

    private static int Compare<TTestCollection>(TTestCollection x, TTestCollection y)
        where TTestCollection : ITestCollection
    {
        // FlakeyTests are ordered before default test collection.
        var xIsFlaky = x.TestCollectionDisplayName == Constants.FlakyTests;
        var yIsFlaky = y.TestCollectionDisplayName == Constants.FlakyTests;
        if (xIsFlaky != yIsFlaky)
            return xIsFlaky ? -1 : 1;

        // LongRunninTests are ordered after default test collection.
        var xIsLongRunning = x.TestCollectionDisplayName == Constants.LongRunningTests;
        var yIsLongRunning = y.TestCollectionDisplayName == Constants.LongRunningTests;
        if (xIsLongRunning != yIsLongRunning)
            return xIsLongRunning ? 1 : -1;

        // Following logics are same as DefaultTestCollectionOrderer
        var xHash = x.UniqueID.GetHashCode();
        var yHash = y.UniqueID.GetHashCode();

        if (xHash == yHash)
            return 0;
        if (xHash < yHash)
            return -1;
        return 1;
    }
}
