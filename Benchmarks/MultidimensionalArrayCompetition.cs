using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class MultidimensionalArrayCompetition
    {
        private const int N = 100, M = 100, IterationCount = 100000;
        private int[] single;
        private int[][] jagged;
        private int[,] rectangular;

        [BenchmarkMethodInitialize]
        public void SingleInitialize()
        {
            single = new int[N * M];
        }

        [BenchmarkMethodClean]
        public void SingleClean()
        {
            single = null;
        }

        [BenchmarkMethodInitialize]
        public void JaggedInitialize()
        {
            jagged = new int[N][];
            for (int i = 0; i < N; i++)
                jagged[i] = new int[M];
        }

        [BenchmarkMethodClean]
        public void JaggedClean()
        {
            jagged = null;
        }

        [BenchmarkMethodInitialize]
        public void RectangularInitialize()
        {
            rectangular = new int[N, M];
        }

        [BenchmarkMethodClean]
        public void RectangularClean()
        {
            rectangular = null;
        }

        [BenchmarkMethod]
        public int Single()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                        sum += single[i * M + j];
            return sum;
        }

        [BenchmarkMethod]
        public int Jagged()
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                        sum += jagged[i][j];
            return sum;
        }

        [BenchmarkMethod]
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