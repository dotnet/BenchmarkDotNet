using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.Introduction
{
    // Also you can define set of tasks for a class.
    // In this case, all of these tasks will apply for all of the benchmark methods
    [BenchmarkTask(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Intro_02_ClassTasks
    {
        // LegacyJIT-x64 can unroll a loop with 1000 iterations and increase performance.
        // LegacyJIT-x86 and RyuJIT can't do it.
        [Benchmark]
        public int Loop1000()
        {
            int sum = 0;
            for (int i = 0; i < 1000; i++)
                sum++;
            return sum;
        }

        // If the amount of iterations is odd, LegacyJit x64 can't do unroll.
        // Thus, all of the JITs show the same results.
        [Benchmark]
        public int Loop1001()
        {
            int sum = 0;
            for (int i = 0; i < 1001; i++)
                sum++;
            return sum;
        }
    }
}