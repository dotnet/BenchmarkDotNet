using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
    [LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
    public class Jit_LoopUnrolling
    {
        [Benchmark]
        public int Sum()
        {
            int sum = 0;
            for (int i = 0; i < 1024; i++)
                sum += i;
            return sum;
        }
    }
}