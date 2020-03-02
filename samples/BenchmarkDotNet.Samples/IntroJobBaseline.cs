using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Net461, baseline: true)]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.Mono)]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.NetCoreApp21)]
    public class IntroJobBaseline
    {
        [Benchmark]
        public int SplitJoin()
            => string.Join(",", new string[1000]).Split(',').Length;
    }
}