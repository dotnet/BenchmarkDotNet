using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    // See http://en.wikipedia.org/wiki/Instruction-level_parallelism
    [Task(mode: BenchmarkMode.SingleRun, platform: BenchmarkPlatform.X86)]
    [Task(mode: BenchmarkMode.SingleRun, platform: BenchmarkPlatform.X64)]
    [Task(mode: BenchmarkMode.SingleRun, platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Cpu_InstructionLevelParallelism
    {
        private const int IterationCount = 400000001;

        private readonly int[] a = new int[4];

        [Benchmark]
        public int[] Parallel()
        {
            for (int iteration = 0; iteration < IterationCount; iteration++)
            {
                a[0]++;
                a[1]++;
                a[2]++;
                a[3]++;
            }
            return a;
        }

        [Benchmark]
        public int[] Sequential()
        {
            for (int iteration = 0; iteration < IterationCount; iteration++)
                a[0]++;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                a[1]++;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                a[2]++;
            for (int iteration = 0; iteration < IterationCount; iteration++)
                a[3]++;
            return a;
        }
    }
}