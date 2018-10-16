using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.ListBenchmarks {
    internal class FlatBenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        public void Print(IEnumerable<string> testName)
        {
            foreach (string test in testName)
            {
                Console.WriteLine(test);
            }
        }
    }
}