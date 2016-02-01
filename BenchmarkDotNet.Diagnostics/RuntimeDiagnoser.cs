using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Diagnostics
{
    public class RuntimeDiagnoser : DiagnoserBase, IDiagnoser
    {
        private List<OutputLine> Output = new List<OutputLine>();

        public void Start(Benchmark benchmark)
        {
            // Do nothing
        }

        public void Stop(ExecuteResult result)
        {
            // Do nothing
        }

        public void ProcessStarted(Process process)
        {
            // Do nothing
        }

        public void AfterBenchmarkHasRun(Benchmark benchmark, Process process)
        {
            var result = PrintCodeForMethod(benchmark, process, printAssembly: false, printIL: false, printDiagnostics: true);
            Output.AddRange(result);
        }

        public void ProcessStopped(Process process)
        {
            // Do nothing
        }

        public void DisplayResults(ILogger logger)
        {
            foreach (var line in Output)
                logger.Write(line.Kind, line.Text);
        }
    }
}