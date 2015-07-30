using System;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{    
    // Math.Sqrt method uses different ASM instruction of different JIT versions:
    // LegacyJit x86: fsqrt   (FPU)
    // LegacyJit x64: sqrtsd  (SSE2)
    // RyuJIT    x64: vsqrtsd (AVX)
    [BenchmarkTask(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Math_DoubleSqrt
    {
        private int counter;
        private double x = 42;

        [Benchmark]
        public double SqrtX()
        {
            counter = (counter + 1) % 100;
            return Math.Sqrt(counter);
        }

        // See also: http://stackoverflow.com/questions/8847429/sse-slower-than-fpu
        // See also: http://stackoverflow.com/questions/8924729/using-avx-intrinsics-instead-of-sse-does-not-improve-speed-why
        // See also: http://www.agner.org/optimize/instruction_tables.pdf
        // See also: http://assemblyrequired.crashworks.org/timing-square-root/
    }
}