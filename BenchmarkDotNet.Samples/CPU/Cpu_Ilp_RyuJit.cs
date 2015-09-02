using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.CPU
{
    // See: https://github.com/dotnet/coreclr/issues/993
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Cpu_Ilp_RyuJit
    {
        private double[] x = new double[11];

        [Benchmark]
        public double Calc()
        {
            double sum = 0.0;
            for (int i = 1; i < x.Length; i++)
                sum += 1.0 / (i * i) * x[i];
            return sum;
        }
    }
}