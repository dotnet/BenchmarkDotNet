using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class FrameworkVersionHelperTests
    {
        [Theory]
        [InlineData("4.6.1", "4.6.1")]
        [InlineData("4.6.1.123", "4.6.1")]
        [InlineData("4.6.2", "4.6.2")]
        [InlineData("4.6.2.123", "4.6.2")]
        [InlineData("4.7", "4.7")]
        [InlineData("4.7.0", "4.7")]
        [InlineData("4.7.1", "4.7.1")]
        [InlineData("4.7.1.1243", "4.7.1")]
        [InlineData("4.7.2", "4.7.2")]
        [InlineData("4.7.2.1243", "4.7.2")]
        [InlineData("4.7.3324.0", "4.7.2")]
        [InlineData("4.8", "4.8")]
        [InlineData("4.8.024", "4.8")]
        [InlineData("4.8.4510.0", "4.8")]
        [InlineData("4.8.4526.0", "4.8")]
        [InlineData("4.8.9032.0", "4.8.1")]
        public void ServicingVersionsAreMappedToCorrespondingReleaseVersions(string servicingVersion, string expectedReleaseVersion)
        {
            Assert.Equal(expectedReleaseVersion, FrameworkVersionHelper.MapToReleaseVersion(servicingVersion));
        }
    }
}
