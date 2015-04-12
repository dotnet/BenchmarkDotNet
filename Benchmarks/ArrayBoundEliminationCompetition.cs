using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class ArrayBoundEliminationCompetition
    {
        private const int N = 100001, IterationCount = 100001;
        private readonly int[] a = new int[N];

        [Benchmark]
        public int SumN()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    sum += a[i];
            return sum;
        }

        [Benchmark]
        public int SumLength()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < a.Length; i++)
                    sum += a[i];
            return sum;
        }
    }
}