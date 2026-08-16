using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    internal class FlatBenchmarkCasesPrinter : IBenchmarkCasesPrinter
    {
        public void Print(IEnumerable<BenchmarkCase> benchmarkCases, ILogger logger)
        {
            var testNames = benchmarkCases
                .Select(p => p.Descriptor.GetFilterName())
                .Distinct();

            foreach (string test in testNames)
            {
                logger.WriteLine(test);
            }
        }
    }
}