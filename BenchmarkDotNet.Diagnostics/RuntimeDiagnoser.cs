using System.Diagnostics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnostics
{
    public class RuntimeDiagnoser : DiagnoserBase, IDiagnoser
    {
        public void Print(Benchmark benchmark, Process process, ILogger logger) => 
            PrintCodeForMethod(benchmark, process, logger, false, false, true);
    }
}