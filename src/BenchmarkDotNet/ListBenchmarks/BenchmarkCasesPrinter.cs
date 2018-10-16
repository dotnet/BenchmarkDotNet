using System.Collections.Generic;

namespace BenchmarkDotNet.ListBenchmarks
{
    internal class BenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        private readonly IBenchmarkCasesPrinter printer;
        
        public BenchmarkCasesPrinter(ListBenchmarkCaseMode listBenchmarkCaseMode)
        {
            switch (listBenchmarkCaseMode)
            {
                case ListBenchmarkCaseMode.Tree:
                    printer = new TreeBenchmarkCasesPrinter();
                    break;
                default:
                    printer = new FlatBenchmarkCasesPrinter();
                    break;
            }
        }

        public void Print(IEnumerable<string> testName)
        {
            printer.Print(testName);
        }
    }
}