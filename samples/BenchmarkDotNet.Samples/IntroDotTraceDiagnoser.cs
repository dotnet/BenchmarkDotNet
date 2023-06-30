using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.dotTrace;

namespace BenchmarkDotNet.Samples
{
    // Enables dotTrace profiling for all jobs
    [DotTraceDiagnoser]
    // Adds the default "external-process" job
    // Profiling is performed using dotTrace command-line Tools
    // See: https://www.jetbrains.com/help/profiler/Performance_Profiling__Profiling_Using_the_Command_Line.html
    [SimpleJob]
    // Adds an "in-process" job
    // Profiling is performed using dotTrace SelfApi
    // NuGet reference: https://www.nuget.org/packages/JetBrains.Profiler.SelfApi
    [InProcess]
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