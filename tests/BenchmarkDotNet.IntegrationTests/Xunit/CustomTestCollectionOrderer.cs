using Xunit.Sdk;

namespace BenchmarkDotNet.IntegrationTests.Xunit;

/// <summary>
/// Custom TestCollection orderer implementation.
/// Run the test collection marked `FlakyTests` with priority.
/// </summary>
internal sealed class CustomTestCollectionOrderer : ITestCollectionOrderer
{
    public const string TypeName = "BenchmarkDotNet.IntegrationTests.Xunit.CustomTestCollectionOrderer";

    public const string AssembyName = "BenchmarkDotNet.IntegrationTests";

    /// <inheritdoc/>
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> TestCollections)
    {
        var result = TestCollections.ToList();
        result.Sort(Compare);
        return result;
    }

    private static int Compare<TTestCollection>(TTestCollection x, TTestCollection y)
        where TTestCollection : ITestCollection
    {
        // FlakeyTests are ordered before default test collection.
        var xIsFlaky = x.DisplayName == Constants.FlakyTests;
        var yIsFlaky = y.DisplayName == Constants.FlakyTests;
        if (xIsFlaky != yIsFlaky)
            return xIsFlaky ? -1 : 1;

        // LongRunninTests are ordered after default test collection.
        var xIsLongRunning = x.DisplayName == Constants.LongRunningTests;
        var yIsLongRunning = y.DisplayName == Constants.LongRunningTests;
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
