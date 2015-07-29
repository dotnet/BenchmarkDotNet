using BenchmarkDotNet.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Samples
{
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Jit_BoolToInt
    {
        private const int N = 1001;
        private readonly bool[] bits;

        public Jit_BoolToInt()
        {
            bits = new bool[N];
            for (int i = 0; i < N; i++)
                bits[i] = i % 2 == 0;
        }

        [Benchmark]
        public int Framework()
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
                sum += Convert.ToInt32(bits[i]);

            return sum;
        }

        [Benchmark]
        public int IfThenElse()
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
                sum += bits[i] ? 1 : 0;

            return sum;
        }

        [Benchmark]
        public int UnsafeConvert()
        {
            int sum = 0;
            unsafe
            {
                for (int i = 0; i < N; i++)
                {
                    bool v = bits[i];
                    sum += *(int*)(&v);
                }
            }
            return sum;
        }
    }
}
