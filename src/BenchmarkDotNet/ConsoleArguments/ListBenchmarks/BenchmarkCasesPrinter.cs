using System.Collections.Generic;

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

        public void Print(IEnumerable<string> testName) => printer.Print(testName);
    }
}