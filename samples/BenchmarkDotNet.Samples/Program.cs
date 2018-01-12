using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    public class Bench
    {
        [Benchmark(Description = "Benchmarking Container.Resolve<SomeType>()")]
        public void Foo() {}
    }
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Bench>();
//            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}