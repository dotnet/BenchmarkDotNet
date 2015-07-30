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
        private bool p1, p2, p3, p4, p5, p6;
        public int q1, q2, q3, q4, q5, q6;

        public Jit_BoolToInt()
        {
            p1 = p3 = p5 = true;
            p2 = p4 = p6 = false;
        }

        [Benchmark]
        [OperationsPerInvoke(6)]
        public void Framework()
        {
            q1 = Convert.ToInt32(p1);
            q2 = Convert.ToInt32(p2);
            q3 = Convert.ToInt32(p3);
            q4 = Convert.ToInt32(p4);
            q5 = Convert.ToInt32(p5);
            q6 = Convert.ToInt32(p6);
        }

        [Benchmark]
        [OperationsPerInvoke(6)]
        public void IfThenElse()
        {
            q1 = p1 ? 1 : 0;
            q2 = p2 ? 1 : 0;
            q3 = p3 ? 1 : 0;
            q4 = p4 ? 1 : 0;
            q5 = p5 ? 1 : 0;
            q6 = p6 ? 1 : 0;
        }

        [Benchmark]
        [OperationsPerInvoke(6)]
        public void UnsafeConvert()
        {
            unsafe
            {
                bool v1 = p1, v2 = p2, v3 = p3, v4 = p4, v5 = p5, v6 = p6;
                q1 = *(byte*)(&v1);
                q2 = *(byte*)(&v2);
                q3 = *(byte*)(&v3);
                q4 = *(byte*)(&v4);
                q5 = *(byte*)(&v5);
                q6 = *(byte*)(&v6);
            }
        }
    }
}
