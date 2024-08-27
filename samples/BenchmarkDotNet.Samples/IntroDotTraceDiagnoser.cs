using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.dotTrace;

namespace BenchmarkDotNet.Samples
{
    // Profile benchmarks via dotTrace SelfApi profiling for all jobs
    // See: https://www.nuget.org/packages/JetBrains.Profiler.SelfApi
    [DotTraceDiagnoser]
    [SimpleJob] // external-process execution
    [InProcess] // in-process execution
    public class IntroDotTraceDiagnoser
    {
        [Benchmark]
        public void Fibonacci() => Fibonacci(30);

        private static int Fibonacci(int n)
        {
            return n <= 1 ? n : Fibonacci(n - 1) + Fibonacci(n - 2);
        }
    }
}