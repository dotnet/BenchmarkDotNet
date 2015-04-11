using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class MultidimensionalArrayCompetition
    {
        private const int N = 100, M = 100, IterationCount = 100000;
        private int[] single;
        private int[][] jagged;
        private int[,] rectangular;

        [BenchmarkInitialize]
        public void SingleInitialize()
        {
            single = new int[N * M];
        }

        [BenchmarkClean]
        public void SingleClean()
        {
            single = null;
        }

        [BenchmarkInitialize]
        public void JaggedInitialize()
        {
            jagged = new int[N][];
            for (int i = 0; i < N; i++)
                jagged[i] = new int[M];
        }

        [BenchmarkClean]
        public void JaggedClean()
        {
            jagged = null;
        }

        [BenchmarkInitialize]
        public void RectangularInitialize()
        {
            rectangular = new int[N, M];
        }

        [BenchmarkClean]
        public void RectangularClean()
        {
            rectangular = null;
        }

        [Benchmark]
        public int Single()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                        sum += single[i * M + j];
            return sum;
        }

        [Benchmark]
        public int Jagged()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                        sum += jagged[i][j];
            return sum;
        }

        [Benchmark]
        public int Rectangular()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                        sum += rectangular[i, j];
            return sum;
        }
    }
}