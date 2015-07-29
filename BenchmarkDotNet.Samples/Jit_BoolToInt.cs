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
        private bool first;
        private bool second;

        public Jit_BoolToInt()
        {
            first = true;
            second = false;
        }

        [Benchmark]
        public int Framework()
        {
            int sum = Convert.ToInt32(first);
            sum += Convert.ToInt32(second);
            return sum;
        }

        [Benchmark]
        public int IfThenElse()
        {
            int sum = first ? 1 : 0;
            sum += second ? 1 : 0;
            return sum;
        }

        [Benchmark]
        public int UnsafeConvert()
        {
            unsafe
            {
                bool v1 = first;
                int sum = *(int*)(&v1);

                bool v2 = second;
                sum += *(int*)(&v2);
                return sum;
            }
        }
    }
}
