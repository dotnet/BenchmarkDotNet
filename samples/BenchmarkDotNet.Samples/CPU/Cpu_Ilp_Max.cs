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
                Add(new Job(Job.LegacyJitX86) { Run = { TargetCount = 20} });
            }
        }

        private int[] x = new int[32];

        [GlobalSetup]
        public void Setup ()
        {
            var r = new Random(100);
            for (int i = 0; i < x.Length; i++)
                x[i] = r.Next();
        }

        [Benchmark]
        public int Max()
        {
            var y = x;
            int max = int.MinValue;
            for (int i = 0; i < y.Length; i++)
                max = Math.Max(max, y[i]); // Math.Max should be similar to MaxBranchNoStore because many microarchitectures do not have an opcode for max
            return max;
        }

        [Benchmark]
        public int MaxEvenOdd()
        {
            var y = x;
            int maxEven = int.MinValue, maxOdd = int.MinValue;
            for (int i = 0; i < y.Length; i += 2)
            {
                // this gets rid of some pipeline stalls caused by store-load dependencies that would happen immediately
                // and also gets rid of half of the jumps, compares and additions of the for-loop doing some loop unrolling. 
                maxEven = Math.Max(maxEven, y[i]);
                maxOdd = Math.Max(maxOdd, y[i + 1]);
            }
            return Math.Max(maxEven, maxOdd);
        }

        [Benchmark]
        public unsafe int MaxBranchless()
        {
            var y = x;
            int max = int.MinValue;
            for (int i = 0; i < y.Length; i++)
            {
                // This will introduce lots of store-load dependencies causing serious pipeline stalls.          
                bool branch = max < y[i]; 
                max = max ^ ((max ^ y[i]) & -*((byte*)(&branch)));
            }
            return max;
        }

        [Benchmark]
        public int MaxBranch()
        {
            var y = x;

            int max = int.MinValue;
            for (int i = 0; i < y.Length; i++)
                max = max < y[i] ? y[i] : max; // This will introduce a store-load dependency causing a pipeline stall. 
            return max;
        }

        [Benchmark]
        public int MaxBranchNoStore()
        {
            var y = x;

            int max = int.MinValue;
            for (int i = 0; i < y.Length; i++)
            {
                // The probability that the branch prediction fails, becomes smaller but not 0 over time. 
                if (max < y[i])
                    max = y[i];
            }

            return max;
        }
    }
}