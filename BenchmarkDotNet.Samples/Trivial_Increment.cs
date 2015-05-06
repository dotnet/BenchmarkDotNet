using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    // See: http://en.wikipedia.org/wiki/Increment_and_decrement_operators
    public class Trivial_Increment
    {
        private const int IterationCount = 1000000001;

        [Benchmark("i++")]
        public int PostIncrement()
        {
            int x = 0;
            for (int i = 0; i < 1000000001; i++)
                x++;
            return x;
        }

        [Benchmark("++")]
        public int PreIncrement()
        {
            int x = 0;
            for (int i = 0; i < 1000000001; i++)
                x++;
            return x;
        }
    }
}