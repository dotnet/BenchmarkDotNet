using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
    // See: https://alexandrnikitin.github.io/blog/dotnet-generics-under-the-hood/
    [AllJitsJob]
    public class Jit_GenericsMethod
    {
        private class BaseClass<T>
        {
            private List<T> list = new List<T>();

            public BaseClass()
            {
                foreach (var _ in list) { }
            }

            public void Run()
            {
                for (var i = 0; i < 11; i++)
                    if (list.Any(_ => true))
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