using System;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class MultidimensionalArrayBenchmark
    {
        private const int N = 100, M = 100, IterationCount = 100000;
        private int[] single;
        private int[][] jagged;
        private int[,] rectangular;

        public void Run()
        {
            var competition = new BenchmarkCompetition();

            competition.AddTask("Single",
                () => single = new int[N * M],
                () => SingleRun(single));

            competition.AddTask("Jagged",
                () =>
                {
                    jagged = new int[N][];
                    for (int i = 0; i < N; i++)
                        jagged[i] = new int[M];
                },
                () => JaggedRun(jagged));

            competition.AddTask("Rectangular",
                () => rectangular = new int[N, M],
                () => RectangularRun(rectangular));

            competition.Run();
        }

        private int SingleRun(int[] a)
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                        sum += a[i * M + j];
            return sum;
        }

        private int JaggedRun(int[][] a)
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                        sum += a[i][j];
            return sum;
        }

        private int RectangularRun(int[,] a)
        {
            int sum = 0;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                        sum += a[i, j];
            return sum;
        }
    }
}