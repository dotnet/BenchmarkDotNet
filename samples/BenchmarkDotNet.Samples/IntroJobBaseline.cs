using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [TargetFrameworkJob(TargetFrameworkMoniker.Net461)]
    [TargetFrameworkJob(TargetFrameworkMoniker.Mono)]
    [TargetFrameworkJob(TargetFrameworkMoniker.Netcoreapp21)]
    public class IntroJobBaseline
    {
        [Benchmark]
        public int SplitJoin() 
            => string.Join(",", new string[1000]).Split(',').Length;
    }
}