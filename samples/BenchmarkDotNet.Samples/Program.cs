using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<Bench>();
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }

    [ShortRunJob]
    [PhdExporter]
    [JsonExporter]
    public class Bench
    {
        [Benchmark]
        public void Foo() => Thread.Sleep(10);

        [Benchmark]
        public void Bar() => Thread.Sleep(20);
    }
}