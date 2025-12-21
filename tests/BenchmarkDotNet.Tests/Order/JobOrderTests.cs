using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.Tests.Order;

public class JobOrderTests
{
    [Fact]
    public void TestJobOrders_ByJobId()
    {
        // Arrange
        Job[] jobs =
        [
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp80)
                   .WithRuntime(CoreRuntime.Core80)
                   .WithId("v1.4.1"),
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp90)
                   .WithRuntime(CoreRuntime.Core90)
                   .WithId("v1.4.10"),
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp10_0)
                   .WithRuntime(CoreRuntime.Core10_0)
                   .WithId("v1.4.2"),
        ];

        // Verify jobs are sorted by JobId's default order (numeric order).
        {
            // Act
            var comparer = JobComparer.Default;
            var results = jobs.OrderBy(x => x, comparer)
                              .Select(x => x.Job.Id)
                              .ToArray();

            // Assert
            Assert.Equal(["v1.4.1", "v1.4.2", "v1.4.10"], results);
        }

        // Verify jobs are sorted by JobId's ordinal order.
        {
            // Act
            var comparer = JobComparer.Ordinal;
            var results = jobs.OrderBy(d => d, comparer)
                              .Select(x => x.Job.Id)
                              .ToArray();
            // Assert
            Assert.Equal(["v1.4.1", "v1.4.10", "v1.4.2"], results);
        }
    }

    [Fact]
    public void TestJobOrders_ByRuntime()
    {
        // Arrange
        Job[] jobs =
        [
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp10_0)
                   .WithRuntime(CoreRuntime.Core80),
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp90)
                   .WithRuntime(CoreRuntime.Core90),
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp80)
                   .WithRuntime(CoreRuntime.Core10_0),
        ];

        // Act
        // Verify jobs are sorted by Runtime name order.
        var results = jobs.OrderBy(d => d, JobComparer.Default)
                          .Select(x => x.Job.Environment.GetRuntime().Name)
                          .ToArray();

        // Assert
        var expected = new[]
        {
            CoreRuntime.Core80.Name,
            CoreRuntime.Core90.Name,
            CoreRuntime.Core10_0.Name
        };
        Assert.Equal(expected, results);
    }

    [Fact]
    public void TestJobOrders_ByToolchain()
    {
        // Arrange
        Job[] jobs =
        [
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp10_0),
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp90),
            Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp80),
        ];

        // Act
        // Verify jobs are sorted by Toolchain name order.
        var results = jobs.OrderBy(d => d, JobComparer.Default)
                          .Select(x => x.Job.GetToolchain().Name)
                          .ToArray();

        // Assert
        var expected = new[]
        {
            CsProjCoreToolchain.NetCoreApp80.Name,
            CsProjCoreToolchain.NetCoreApp90.Name,
            CsProjCoreToolchain.NetCoreApp10_0.Name,
        };
        Assert.Equal(expected, results);
    }

    [Theory]
    [InlineData("item1", "item1", 0)]
    [InlineData("item123", "item123", 0)]
    // Compare different values
    [InlineData("item1", "item2", -1)]
    [InlineData("item2", "item1", 1)]
    [InlineData("item2", "item10", -1)]
    [InlineData("item10", "item2", 1)]
    [InlineData("item1a", "item1b", -1)]
    [InlineData("item1b", "item1a", 1)]
    [InlineData("item", "item1", -1)]
    [InlineData("item10", "item", 1)]
    [InlineData(".NET 8", ".NET 10", -1)]
    [InlineData(".NET 10", ".NET 8", 1)]
    [InlineData("v1.4.1", "v1.4.10", -1)]
    [InlineData("v1.4.10", "v1.4.2", 1)]
    // Compare zero paddeed numeric string.
    [InlineData("item01", "item1", 0)]
    [InlineData("item001", "item1", 0)]
    [InlineData("item1", "item001", 0)]
    [InlineData("item1", "item01", 0)]
    [InlineData("item9", "item09", 0)]
    [InlineData(".NET 08", ".NET 10", -1)]
    [InlineData(".NET 10", ".NET 08", 1)]
    // Arguments that contains null
    [InlineData("", "a", -1)]
    [InlineData("a", "", 1)]
    [InlineData("", "", 0)]
    public void TestNumericComparer(string a, string b, int expectedSign)
    {
        var jobA = new Job(a);
        var jobB = new Job(b);

        int result = JobComparer.Default.Compare(jobA, jobB);
        Assert.Equal(expectedSign, NormalizeSign(result));

        static int NormalizeSign(int value)
            => value == 0 ? 0 : value < 0 ? -1 : 1;
    }
}
