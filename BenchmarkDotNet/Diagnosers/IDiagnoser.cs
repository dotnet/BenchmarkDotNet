using System.Diagnostics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Diagnosers
{
    /// The events are guaranteed to happen in the following sequence:
    /// Start                  // When the Benchmark run is started and most importantly BEFORE the process has been launched
    /// ProcessStarted         // After the Process (in a "Diagnostic" run) has been launched
    /// AfterBenchmarkHasRun   // After a "Warmpup" iteration of the Benchmark has run, i.e. we know the [Benchmark] method has been 
    ///                        // executed and JITted, this is important if the Diagnoser needs to know when it can do a Memory Dump.
    /// ProcessStopped         // Once the Process (in a "Diagnostic" run) has stopped/completed
    /// Stop                   // At the end, when the entire Benchmark run has complete
    /// DisplayResults         // When the results/output should be displayed
    public interface IDiagnoser
    {
        void Start(Benchmark benchmark);

        void Stop(ExecuteResult result);

        void ProcessStarted(Process process);

        void AfterBenchmarkHasRun(Benchmark benchmark, Process process);

        void ProcessStopped(Process process);

        void DisplayResults(ILogger logger);
    }
}
