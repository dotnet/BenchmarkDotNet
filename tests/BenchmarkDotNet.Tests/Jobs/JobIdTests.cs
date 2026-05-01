using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Tests.Jobs;

public class JobIdTests
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

    public static TheoryData<string, Job> GetTheoryData() => new()
    {
        {"InProcess", Job.InProcess },

        // If baseJob don't have id. Set "InProcess".
        {"InProcess", InProcessAttribute.GetJob(Job.Default, InProcessToolchainType.Auto, true) },

        // If baseJob has exisiting id, it should not be overwritten.
        {"Dry", InProcessAttribute.GetJob(Job.Dry, InProcessToolchainType.Auto, true) },
    };
}
