using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.IntegrationTests;

public class IntegrationTestSetupTests
{
    [Fact]
    public void IntegrationTestsAreDetected() => Assert.True(XUnitHelper.IsIntegrationTest.Value);
}