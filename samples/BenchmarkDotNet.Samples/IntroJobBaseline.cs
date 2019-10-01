using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [TargetFrameworkJob(TargetFrameworkMoniker.Net461, baseline: true)]
    [TargetFrameworkJob(TargetFrameworkMoniker.Mono)]
    [TargetFrameworkJob(TargetFrameworkMoniker.NetCoreApp21)]
    public class IntroJobBaseline
    {
        [Benchmark]
        public int SplitJoin()
            => string.Join(",", new string[1000]).Split(',').Length;
    }
}