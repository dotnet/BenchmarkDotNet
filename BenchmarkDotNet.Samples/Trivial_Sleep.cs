using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class Trivial_Sleep
    {
        [Benchmark]
        public void Sleep10()
        {
            Thread.Sleep(10);
        }

        [Benchmark]
        public void Sleep100()
        {
            Thread.Sleep(100);
        }
    }
}