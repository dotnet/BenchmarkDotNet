using System.Diagnostics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Diagnosers
{
    public class CompositeDiagnoser : IDiagnoser
    {
        private readonly IDiagnoser[] diagnosers;

        public CompositeDiagnoser(params IDiagnoser[] diagnosers)
        {
            this.diagnosers = diagnosers;
        }

        public void Start(Benchmark benchmark)
        {
            foreach (var diagnoser in diagnosers)
                diagnoser.Start(benchmark);
        }

        public void Stop(Benchmark benchmark, BenchmarkReport report)
        {
            foreach (var diagnoser in diagnosers)
                diagnoser.Stop(benchmark, report);
        }

        public void ProcessStarted(Process process)
        {
            foreach (var diagnoser in diagnosers)
                diagnoser.ProcessStarted(process);
        }

        public void AfterBenchmarkHasRun(Benchmark benchmark, Process process)
        {
            foreach (var diagnoser in diagnosers)
                diagnoser.AfterBenchmarkHasRun(benchmark, process);
        }

        public void ProcessStopped(Process process)
        {
            foreach (var diagnoser in diagnosers)
                diagnoser.ProcessStopped(process);
        }

        public void DisplayResults(ILogger logger)
        {
            foreach (var diagnoser in diagnosers)
            {
                // TODO when Diagnosers/Diagnostis are wired up properly, instead of the Type name, 
                // print the name used on the cmd line, i.e. -d=<NAME>
                logger.WriteLineHeader($"// * Diagnostic Output - {diagnoser.GetType().Name} *");
                diagnoser.DisplayResults(logger);
                logger.WriteLine();
            }
        }
    }
}