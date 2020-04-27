using System.Collections.Generic;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal class FlatBenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        public void Print(IEnumerable<string> testNames, ILogger logger)
        {
            foreach (string test in testNames)
            {
                logger.WriteLine(test);
            }
        }
    }
}