using System.Threading;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.Introduction
{
    [BenchmarkTask(runtime: BenchmarkRuntime.Clr)]
    [BenchmarkTask(runtime: BenchmarkRuntime.Mono)]
    public class Intro_06_Runtime
    {
        [Benchmark]
        public void Sleep()
        {
            Thread.Sleep(100);
        }
    }
}