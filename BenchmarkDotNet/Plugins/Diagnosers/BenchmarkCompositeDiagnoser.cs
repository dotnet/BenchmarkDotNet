using System.Diagnostics;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins.Diagnosers
{
    public class BenchmarkCompositeDiagnoser : IBenchmarkDiagnoser
    {
        public string Name => "composite";
        public string Description => "Composite Diagnoser";

        private readonly IBenchmarkDiagnoser[] diagnosers;

        public BenchmarkCompositeDiagnoser(params IBenchmarkDiagnoser[] diagnosers)
        {
            this.diagnosers = diagnosers;
        }

        public void Print(Benchmark benchmark, Process process, IBenchmarkLogger logger)
        {
            foreach (var diagnoster in diagnosers)
                diagnoster.Print(benchmark, process, logger);
        }
    }
}