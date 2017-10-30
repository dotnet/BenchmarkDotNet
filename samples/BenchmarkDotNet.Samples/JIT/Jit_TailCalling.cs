using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
#if !CORE
    [Diagnostics.Windows.Configs.TailCallDiagnoser]
#endif
    [LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
    //See https://en.wikipedia.org/wiki/Tail_call
    public class Jit_TailCalling
    {
        [Benchmark]
        public long Calc()
        {
            return FactorialWithoutTailing(7) - FactorialWithTailing(7);
        }

        private static long FactorialWithoutTailing(int depth)
        {
            return depth == 0 ? 1 : depth * FactorialWithoutTailing(depth - 1);
        }

        private static long FactorialWithTailing(int pos, int depth)
        {
            return pos == 0 ? depth : FactorialWithTailing(pos - 1, depth * pos);
        }

        private static long FactorialWithTailing(int depth)
        {
            return FactorialWithTailing(1, depth);
        }
    }
}
