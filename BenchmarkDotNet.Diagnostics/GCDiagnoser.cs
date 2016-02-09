using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Diagnostics
{
    public class GCDiagnoser : ETWDiagnoser, IDiagnoser, IColumn
    {
        private readonly List<OutputLine> output = new List<OutputLine>();
        private readonly LogCapture logger = new LogCapture();

        public void Start(Benchmark benchmark)
        {
            if (Directory.Exists(ProfilingFolder) == false)
                Directory.CreateDirectory(ProfilingFolder);

            var filePrefix = GetFileName("GC", benchmark, benchmark.Parameters);

            // Clean-up in case a previous run is still going!! We don't have to print the output here, 
            // because it can fail if things worked okay last time (i.e. nothing to clean-up)
            var output = RunProcess("logman", string.Format("stop {0} -ets", filePrefix));
            DeleteIfFileExists(filePrefix + ".etl");

            // 0x00000001 means collect GC Events only, (JIT Events are 0x00000010),
            // see https://msdn.microsoft.com/en-us/library/ff357720(v=vs.110).aspx for full list
            // 0x5 is the "ETW Event Level" and we set it to "Verbose" (and below)
            // Other flags used:
            // -ets                          Send commands to Event Trace Sessions directly without saving or scheduling.
            // -ct <perf|system|cycle>       Specifies the clock resolution to use when logging the time stamp for each event. 
            //                               You can use query performance counter, system time, or CPU cycle.
            var arguments =
                $"start {filePrefix} -o .\\{ProfilingFolder}\\{filePrefix}.etl -p {CLRRuntimeProvider} 0x00000001 0x5 -ets -ct perf";
            output = RunProcess("logman", arguments);
            if (output.Contains(ExecutedOkayMessage) == false)
                logger.WriteLineError("logman start output:\n" + output);
        }

        public void Stop(Benchmark benchmark, BenchmarkReport report)
        {
            var filePrefix = GetFileName("GC", report.Benchmark, benchmark.Parameters);
            var output = RunProcess("logman", string.Format("stop {0} -ets", filePrefix));
            if (output.Contains(ExecutedOkayMessage) == false)
                logger.WriteLineError("logman stop output\n" + output);
        }

        public void ProcessStarted(Process process)
        {
            ProcessIdsUsedInRuns.Add(process.Id);
        }

        public void AfterBenchmarkHasRun(Benchmark benchmark, Process process)
        {
            // Do nothing
        }

        public void ProcessStopped(Process process)
        {
            // Do nothing
        }

        public void DisplayResults(ILogger logger)
        {
            foreach (var line in output)
                logger.Write(line.Kind, line.Text);
        }

        string IColumn.ColumnName => "GC";

        bool IColumn.IsAvailable(Summary summary) => true;

        bool IColumn.AlwaysShow => true;

        string IColumn.GetValue(Summary summary, Benchmark benchmark)
        {
            return "N/A";
        }
    }
}
