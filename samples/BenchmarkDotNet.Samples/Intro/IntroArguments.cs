using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroArguments
    {
        [Benchmark]
        [Arguments(100, 10)]
        [Arguments(100, 20)]
        [Arguments(200, 10)]
        [Arguments(200, 20)]
        public void Benchmark(int a, int b) => Thread.Sleep(a + b + 5);
    }
}