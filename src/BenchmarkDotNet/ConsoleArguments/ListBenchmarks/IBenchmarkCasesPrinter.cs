using System.Collections.Generic;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal interface IBenchmarkCasesPrinter
    {
        void Print(IEnumerable<string> testNames);
    }
}