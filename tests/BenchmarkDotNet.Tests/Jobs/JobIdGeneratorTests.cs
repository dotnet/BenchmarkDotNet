using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.NetCoreApp;

namespace BenchmarkDotNet.Tests.Jobs;

public class JobIdGeneratorTests
{
    [Theory]
    [MemberData(nameof(GetTheoryData), DisableDiscoveryEnumeration = true)]
    public void AutoGenerateJobId(string expectedId, Job job)
    {
        // Act
        var result = job.ResolvedId;

        // Assert
        Assert.Equal(expectedId, result);
    }

    public static TheoryData<string, Job> GetTheoryData() => new TheoryData<string, Job>()
    {
        {"Job-XKQDZR", Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp90) },
        {"Job-CFYQDH", Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp10_0) },
        {"Job-GVKUBM", Job.Default.WithRuntime(CoreRuntime.Core10_0) },
    };
}
