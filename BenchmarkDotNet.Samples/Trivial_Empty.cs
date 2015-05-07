using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class Trivial_Empty
    {
        [Benchmark]
        public void Empty()
        {
        }
    }
}