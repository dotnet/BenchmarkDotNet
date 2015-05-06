using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    // See http://en.wikipedia.org/wiki/Inline_expansion
    // See http://aakinshin.net/en/blog/dotnet/inlining-and-starg/
    [Task(platform: BenchmarkPlatform.X86)]
    [Task(platform: BenchmarkPlatform.X64)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Jit_Inlining
    {
        [Benchmark]
        public int Calc()
        {
            int sum = 0;
            for (int i = 0; i < 1000000001; i++)
                sum += WithoutStarg(0x11) + WithStarg(0x12);
            return sum;
        }

        private static int WithoutStarg(int value)
        {
            return value;
        }

        private static int WithStarg(int value)
        {
            if (value < 0)
                value = -value;
            return value;
        }
    }
}