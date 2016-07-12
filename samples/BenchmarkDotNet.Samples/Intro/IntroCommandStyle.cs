using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config("jobs=Dry columns=StdDev")]
    public class IntroCommandStyle
    {
        [Benchmark]
        public void Benchmark()
        {
            Thread.Sleep(10);
        }
    }
}