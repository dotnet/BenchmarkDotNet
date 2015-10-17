using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.JIT
{
    [BenchmarkTask(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Jit_AsVsCast
    {
        public class Foo
        {
        }

        private object foo = new Foo();

        [Benchmark]
        public Foo As()
        {
            return foo as Foo;
        }

        [Benchmark]
        public object Cast()
        {
            return (Foo)foo;
        }
    }
}