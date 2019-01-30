using System.Collections.Generic;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal interface IBenchmarkCasesPrinter
    {
        void Print(IEnumerable<string> testNames, ILogger logger);
    }
}