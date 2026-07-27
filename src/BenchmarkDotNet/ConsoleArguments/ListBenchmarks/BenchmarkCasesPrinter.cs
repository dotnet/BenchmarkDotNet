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
                _ => new FlatBenchmarkCasesPrinter(),
            };
        }

        public static void PrintList(ILogger nonNullLogger, IConfig effectiveConfig, IReadOnlyList<Type> allAvailableTypesWithRunnableBenchmarks, CommandLineOptions options)
        {
            var printer = new BenchmarkCasesPrinter(options.ListBenchmarkCaseMode);
            var benchmarkCases = TypeFilter
                .Filter(effectiveConfig, allAvailableTypesWithRunnableBenchmarks)
                .SelectMany(p => p.BenchmarksCases);

            printer.Print(benchmarkCases, nonNullLogger);
        }

        public void Print(IEnumerable<BenchmarkCase> benchmarkCases, ILogger logger) => printer.Print(benchmarkCases, logger);
    }
}