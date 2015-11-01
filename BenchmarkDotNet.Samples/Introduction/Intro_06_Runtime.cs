using System.Threading;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.Introduction
{
    [BenchmarkTask(executor: BenchmarkExecutor.Classic, runtime: BenchmarkRuntime.Clr)]
    [BenchmarkTask(executor: BenchmarkExecutor.Classic, runtime: BenchmarkRuntime.Mono)]
    public class Intro_06_Runtime
    {
        [Benchmark]
        public void Sleep()
        {
            Thread.Sleep(100);
        }
    }
}