using BenchmarkDotNet.Properties;
using Xunit;

namespace BenchmarkDotNet.Tests.Properties;

public class BenchmarkDotNetInfoTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("1.2.3.4", "1.2.3.4")]
    [InlineData("1729-foo", "1729-foo")]
    [InlineData("0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a", "0.13.9")]
    [InlineData("1-2+3", "1-2")]
    public void RemoveVersionMetadata(string input, string expectedOutput)
    {
        string? actualOutput = BenchmarkDotNetInfo.RemoveVersionMetadata(input);
        Assert.Equal(expectedOutput, actualOutput);
    }
}