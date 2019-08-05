using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetCore
{
    // this project targets only .NET Core, so setting TFM to Full .NET Framework is going to fail
    [TargetFrameworkJob(Jobs.TargetFrameworkMoniker.Net461)]
    public class BenchmarkFullFramework
    {
        [Benchmark]
        public void Benchmark() { }
    }
}
