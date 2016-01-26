using System.Diagnostics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    public class CompositeDiagnoser : IDiagnoser
    {
        private readonly IDiagnoser[] diagnosers;

        public CompositeDiagnoser(params IDiagnoser[] diagnosers)
        {
            this.diagnosers = diagnosers;
        }

        public void Print(Benchmark benchmark, Process process, ILogger logger)
        {
            foreach (var diagnoster in diagnosers)
                diagnoster.Print(benchmark, process, logger);
        }
    }
}