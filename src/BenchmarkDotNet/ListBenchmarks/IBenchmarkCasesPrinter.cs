using System.Collections.Generic;

namespace BenchmarkDotNet.ListBenchmarks
{
    internal interface IBenchmarkCasesPrinter
    {
        void Print(IEnumerable<string> testName);
    }
}