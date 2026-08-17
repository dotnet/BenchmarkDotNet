using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal interface IBenchmarkCasesPrinter
    {
        void Print(IEnumerable<BenchmarkCase> benchmarkCases, ILogger logger);
    }
}