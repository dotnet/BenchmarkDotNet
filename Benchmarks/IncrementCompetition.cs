using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class IncrementCompetition
    {
        private const int IterationCount = 2000000000;

        [BenchmarkMethod("i++")]
        public int After()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                counter++;
            return counter;
        }

        [BenchmarkMethod("++i")]
        public int Before()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; ++i)
                ++counter;
            return counter;
        }
    }
}