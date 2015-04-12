using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class MatrixMultiplicationCompetition
    {
        private const int N = 1024;
        private readonly double[,] a = new double[N, N];
        private readonly double[,] b = new double[N, N];
        private readonly double[,] c = new double[N, N];

        [Benchmark]
        public void MulIjk()
        {
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                    for (int k = 0; k < N; k++)
                        c[i, k] += a[i, j] * b[j, k];
        }

        [Benchmark]
        public void MulIkj()
        {
            for (int i = 0; i < N; i++)
                for (int k = 0; k < N; k++)
                    for (int j = 0; j < N; j++)
                        c[i, k] += a[i, j] * b[j, k];
        }

        [Benchmark]
        public void MulJik()
        {
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                    for (int k = 0; k < N; k++)
                        c[i, k] += a[i, j] * b[j, k];
        }

        [Benchmark]
        public void MulJki()
        {
            for (int j = 0; j < N; j++)
                for (int k = 0; k < N; k++)
                    for (int i = 0; i < N; i++)
                        c[i, k] += a[i, j] * b[j, k];
        }

        [Benchmark]
        public void MulKij()
        {
            for (int k = 0; k < N; k++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < N; j++)
                        c[i, k] += a[i, j] * b[j, k];
        }

        [Benchmark]
        public void MulKji()
        {
            for (int k = 0; k < N; k++)
                for (int j = 0; j < N; j++)
                    for (int i = 0; i < N; i++)
                        c[i, k] += a[i, j] * b[j, k];
        }
    }
}