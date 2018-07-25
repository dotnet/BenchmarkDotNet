using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroArguments
    {
        [Params(true, false)] // Arguments can be combined with Params
        public bool AddExtra5Milliseconds;

        [Benchmark]
        [Arguments(100, 10)]
        [Arguments(100, 20)]
        [Arguments(200, 10)]
        [Arguments(200, 20)]
        public void Benchmark(int a, int b)
        {
            if (AddExtra5Milliseconds)
                Thread.Sleep(a + b + 5);
            else
                Thread.Sleep(a + b);
        }
    }
}