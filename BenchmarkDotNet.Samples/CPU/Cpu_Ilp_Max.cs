using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.CPU
{
    [Config(typeof(Config))]
    public class Cpu_Ilp_Max
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.LegacyX86.WithTargetCount(20));
            }
        }

        private int[] x = new int[32];

        [Benchmark]
        public int Max()
        {
            var y = x;
            int max = int.MinValue;
            for (int i = 0; i < y.Length; i++)
                max = Math.Max(max, x[i]);
            return max;
        }

        [Benchmark]
        public int MaxEvenOdd()
        {
            var y = x;
            int maxEven = int.MinValue, maxOdd = int.MinValue;
            for (int i = 0; i < y.Length; i += 2)
            {
                maxEven = Math.Max(maxEven, y[i]);
                maxOdd = Math.Max(maxOdd, y[i + 1]);
            }
            return Math.Max(maxEven, maxOdd);
        }
    }
}