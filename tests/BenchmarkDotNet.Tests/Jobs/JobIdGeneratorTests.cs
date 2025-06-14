using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using Xunit;

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
        {"Job-OOTPKI", Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp80) },
        {"Job-QAODSR", Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp90) },
        {"Job-KHMDUZ", Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp80).WithRuntime(CoreRuntime.Core80) },
    };
}
