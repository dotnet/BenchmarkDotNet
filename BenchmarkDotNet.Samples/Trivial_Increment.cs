using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    // See: http://en.wikipedia.org/wiki/Increment_and_decrement_operators
    public class Trivial_Increment
    {
        private double x;

        [Benchmark("x++")]
        public double PostIncrement()
        {
            return x++;
        }

        [Benchmark("++x")]
        public double PreIncrement()
        {
            return ++x;
        }
    }
}