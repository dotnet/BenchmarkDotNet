using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.CPU
{
    // See: https://github.com/dotnet/coreclr/issues/993
    [Config(typeof(Config))]
    public class Cpu_Ilp_RyuJit
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.LegacyX64, Job.RyuJitX64);
            }
        }

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