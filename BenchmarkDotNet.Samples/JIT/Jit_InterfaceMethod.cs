using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.JIT
{
    public class Jit_InterfaceMethod
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.LegacyX86, Job.LegacyX64);
            }
        }

        private interface IFoo
        {
            double Inc(double x);
        }

        private class Foo1 : IFoo
        {
            public double Inc(double x)
            {
                return x + 1;
            }
        }

        private class Foo2 : IFoo
        {
            public double Inc(double x)
            {
                return x + 1;
            }
        }

        private double Run(IFoo foo)
        {
            double sum = 0;
            for (int i = 0; i < 1001; i++)
                sum += foo.Inc(0);
            return sum;
        }

        [Benchmark]
        public double Run1()
        {
            var bar1 = new Foo1();
            var bar2 = new Foo1();
            return Run(bar1) + Run(bar2);
        }

        [Benchmark]
        public double Run2()
        {
            var bar1 = new Foo1();
            var bar2 = new Foo2();
            return Run(bar1) + Run(bar2);
        }

        // See also: http://blogs.msdn.com/b/vancem/archive/2006/03/13/550529.aspx
        // See also: http://www.codeproject.com/Articles/25801/JIT-Optimizations
    }
}