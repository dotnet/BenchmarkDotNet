using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal class BenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        private readonly IBenchmarkCasesPrinter printer;

        public BenchmarkCasesPrinter(ListBenchmarkCaseMode listBenchmarkCaseMode)
        {
            printer = listBenchmarkCaseMode switch
            {
                ListBenchmarkCaseMode.Tree => new TreeBenchmarkCasesPrinter(),
                ListBenchmarkCaseMode.Json => new JsonBenchmarkCasesPrinter(),
                _ => new FlatBenchmarkCasesPrinter(),
            };
        }

        public void Print(IEnumerable<BenchmarkCase> benchmarkCases, ILogger logger) => printer.Print(benchmarkCases, logger);
    }
}