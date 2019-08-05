using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetFramework
{
    // this project targets only Full .NET Framework, so setting TFM to .NET Core is going to fail
    [TargetFrameworkJob(Jobs.TargetFrameworkMoniker.NetCoreApp21)]
    public class BenchmarkDotNetCore
    {
        [Benchmark]
        public void Benchmark() { }
    }
}
