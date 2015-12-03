using System.Diagnostics;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Diagnostics
{
    public class BenchmarkRuntimeDiagnoser : BenchmarkDiagnoserBase, IBenchmarkDiagnoser
    {
        public string Name => "runtime";
        public string Description => "Runtime Diagnoser";

        public void Print(Benchmark benchmark, Process process, IBenchmarkLogger logger)
        {
            PrintCodeForMethod(benchmark, process, logger, false, false, true);
        }
    }
}