using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class Jit_MultidimensionalArrayAccess
    {
        private const int N = 100, M = 100, IterationCount = 100000;
        private int[] single;
        private int[][] jagged;
        private int[,] rectangular;

        public Jit_MultidimensionalArrayAccess()
        {
            single = new int[N * M];
            jagged = new int[N][];
            for (int i = 0; i < N; i++)
                jagged[i] = new int[M];
            rectangular = new int[N, M];
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