using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests;

public class IntegrationTestSetupTests
{
    [Fact]
    public void IntegrationTestsAreDetected() => Assert.True(XUnitHelper.IsIntegrationTest.Value);
}