using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks {
    internal class FlatBenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        public void Print(IEnumerable<string> testNames)
        {
            foreach (string test in testNames)
            {
                Console.WriteLine(test);
            }
        }
    }
}