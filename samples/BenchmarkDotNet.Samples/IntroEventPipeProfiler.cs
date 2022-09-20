using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Samples
{
    [ShortRunJob]
    [EventPipeProfiler(EventPipeProfile.CpuSampling)]
    public class IntroEventPipeProfiler
    {
        [Benchmark]
        public void Sleep() => Thread.Sleep(2000);
    }
}