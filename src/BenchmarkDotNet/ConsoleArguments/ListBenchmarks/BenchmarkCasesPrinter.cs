using System.Collections.Generic;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal class BenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        private readonly IBenchmarkCasesPrinter printer;

        public BenchmarkCasesPrinter(ListBenchmarkCaseMode listBenchmarkCaseMode)
        {
            printer = listBenchmarkCaseMode == ListBenchmarkCaseMode.Tree
                ? (IBenchmarkCasesPrinter) new TreeBenchmarkCasesPrinter()
                : new FlatBenchmarkCasesPrinter();
        }

        public void Print(IEnumerable<string> testNames, ILogger logger) => printer.Print(testNames, logger);
    }
}