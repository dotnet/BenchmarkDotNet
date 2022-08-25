using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroParamsPriority
    {
        [Params(100)]
        public int A { get; set; }

        [Params(10, Priority = -100)]
        public int B { get; set; }

        [Benchmark]
        public void Benchmark() => Thread.Sleep(A + B + 5);
    }
}