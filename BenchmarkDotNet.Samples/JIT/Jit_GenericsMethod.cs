using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.JIT
{
    // See: https://alexandrnikitin.github.io/blog/dotnet-generics-under-the-hood/
    [BenchmarkTask(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Jit_GenericsMethod
    {
        private class BaseClass<T>
        {
            private List<T> list = new List<T>();

            public BaseClass()
            {
                Enumerable.Empty<T>();
            }

            public void Run()
            {
                for (var i = 0; i < 11; i++)
                    if (list.Any())
                        return;
            }
        }

        private class DerivedClass : BaseClass<object>
        {
        }

        private BaseClass<object> baseClass = new BaseClass<object>();
        private BaseClass<object> derivedClass = new DerivedClass();

        [Benchmark]
        public void Base()
        {
            baseClass.Run();
        }

        [Benchmark]
        public void Derived()
        {
            derivedClass.Run();
        }
    }
}