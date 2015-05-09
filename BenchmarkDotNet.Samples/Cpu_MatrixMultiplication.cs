using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    // See http://en.wikipedia.org/wiki/Matrix_multiplication_algorithm#Cache_behavior
    [Task(platform: BenchmarkPlatform.X86)]
    [Task(platform: BenchmarkPlatform.X64)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Cpu_MatrixMultiplication
    {
        private const int N = 512;
        private readonly double[,] a = new double[N, N];
        private readonly double[,] b = new double[N, N];
        private readonly double[,] c = new double[N, N];

        [Benchmark]
        public double[,] MulIjk()
        {
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                    for (int k = 0; k < N; k++)
                        c[i, k] += a[i, j] * b[j, k];
            return c;
        }

        [Benchmark]
        public double[,] MulIkj()
        {
            for (int i = 0; i < N; i++)
                for (int k = 0; k < N; k++)
                    for (int j = 0; j < N; j++)
                        c[i, k] += a[i, j] * b[j, k];
            return c;
        }

        [Benchmark]
        public double[,] MulJik()
        {
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                    for (int k = 0; k < N; k++)
                        c[i, k] += a[i, j] * b[j, k];
            return c;
        }

        [Benchmark]
        public double[,] MulJki()
        {
            for (int j = 0; j < N; j++)
                for (int k = 0; k < N; k++)
                    for (int i = 0; i < N; i++)
                        c[i, k] += a[i, j] * b[j, k];
            return c;
        }

        [Benchmark]
        public double[,] MulKij()
        {
            for (int k = 0; k < N; k++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < N; j++)
                        c[i, k] += a[i, j] * b[j, k];
            return c;
        }

        [Benchmark]
        public double[,] MulKji()
        {
            for (int k = 0; k < N; k++)
                for (int j = 0; j < N; j++)
                    for (int i = 0; i < N; i++)
                        c[i, k] += a[i, j] * b[j, k];
            return c;
        }
    }
}