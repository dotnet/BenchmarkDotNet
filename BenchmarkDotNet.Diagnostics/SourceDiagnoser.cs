using System.Diagnostics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnostics
{
    /// <summary>
    /// Source (IL/ASM) diagnoser
    /// </summary>
    public class SourceDiagnoser : DiagnoserBase, IDiagnoser
    {
        public void Print(Benchmark benchmark, Process process, ILogger logger) => 
            PrintCodeForMethod(benchmark, process, logger, true, true, false);
    }
}