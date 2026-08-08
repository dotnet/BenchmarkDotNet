using AwesomeAssertions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Helpers.Hashing;
using BenchmarkDotNet.Running;
using System.Text;

namespace BenchmarkDotNet.Tests.Helpers;

public class XxHash128Tests
{
    [Fact]
    public void VerifyXxHash128()
    {
        // Arrange
        var bytes = Encoding.ASCII.GetBytes("12345");

        // Act
        var results = XxHash128.Hash(bytes);

        // Caluculated results by using System.IO.Hasing.XxHash128.Hash(bytes);
        byte[] expected = [74, 243, 218, 105, 246, 30, 20, 207, 38, 244, 193, 75, 107, 107, 253, 180];

        // Assert
        results.Length.Should().Be(16);
        results.Should().BeEquivalentTo(expected);
    }
}
