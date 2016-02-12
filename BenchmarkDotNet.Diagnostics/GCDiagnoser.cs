using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Columns;
using Diagnostics.Tracing;
using Diagnostics.Tracing.Parsers;

namespace BenchmarkDotNet.Diagnostics
{
    public class GCDiagnoser : ETWDiagnoser, IDiagnoser, IColumnProvider
    {
        private readonly List<OutputLine> output = new List<OutputLine>();
        private readonly LogCapture logger = new LogCapture();
        private readonly Dictionary<Benchmark, Stats> results = new Dictionary<Benchmark, Stats>();

        public IEnumerable<IColumn> GetColumns => 
            new IColumn[]
            {
                new GCCollectionColumn(results, 0),
                new GCCollectionColumn(results, 1),
                new GCCollectionColumn(results, 2),
                new AllocationColumn(results)
            };

        public void Start(Benchmark benchmark)
        {
            ProcessIdsUsedInRuns.Clear();

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
            var stats = ProcessEtwEvents(benchmark, $".\\{ProfilingFolder}\\{filePrefix}.etl", report.AllMeasurements.Sum(m => m.Operations));
            results.Add(benchmark, stats);
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

        private Stats ProcessEtwEvents(Benchmark benchmark, string fileName, long totalOperations)
        {
            var statsPerProcess = CollectEtwData(fileName);
            if (ProcessIdsUsedInRuns.Count > 0)
            {
                var processToReport = ProcessIdsUsedInRuns[0];
                if (statsPerProcess.ContainsKey(processToReport))
                {
                    var stats = statsPerProcess[processToReport];
                    stats.TotalOperations = totalOperations;
                    return stats;
                }
            }
            return null;
        }

        private Dictionary<int, Stats> CollectEtwData(string fileName)
        {
            var statsPerProcess = new Dictionary<int, Stats>();
            foreach (var process in ProcessIdsUsedInRuns)
                statsPerProcess.Add(process, new Stats());
            using (var source = new ETWTraceEventSource(fileName))
            {
                source.Clr.GCAllocationTick += (gcData =>
                {
                    if (statsPerProcess.ContainsKey(gcData.ProcessID))
                        statsPerProcess[gcData.ProcessID].AllocatedBytes += gcData.AllocationAmount64;
                });

                source.Clr.GCStart += (gcData =>
                {
                    if (statsPerProcess.ContainsKey(gcData.ProcessID))
                    {
                        var genCounts = statsPerProcess[gcData.ProcessID].GenCounts;
                        if (gcData.Depth >= 0 && gcData.Depth < genCounts.Length)
                        {
                            // BenchmarkDotNet calls GC.Collect(..) before/after benchmark runs, ignore these in our results!!!
                            if (gcData.Reason != GCReason.Induced)
                                genCounts[gcData.Depth]++;
                        }
                        else
                        {
                            logger.WriteLineError("Error Process{0}, Unexpected GC Depth: {1}, Count: {2} -> Reason: {3}",
                                                  gcData.ProcessID, gcData.Depth, gcData.Count, gcData.Reason);
                        }
                    }
                });

                source.Process();
            }
            return statsPerProcess;
        }

        public class AllocationColumn : IColumn
        {
            private Dictionary<Benchmark, Stats> results;

            public AllocationColumn(Dictionary<Benchmark, Stats> results)
            {
                this.results = results;
            }

            public string ColumnName => "Memory Traffic/Op";
            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (results.ContainsKey(benchmark) && results[benchmark] != null)
                {
                    var result = results[benchmark];
                    // TODO scale this based on the minimum value in the column, i.e. use B/KB/MB as appropriate
                    return (result.AllocatedBytes / (double)result.TotalOperations).ToString("N2") + " B";
                }
                return "N/A";
            }
        }

        public class GCCollectionColumn : IColumn
        {
            private Dictionary<Benchmark, Stats> results;
            private int generation;
            // TODO also need to find a sensible way of including this in the column name?
            private long opsPerGCCount;

            public GCCollectionColumn(Dictionary<Benchmark, Stats> results, int generation)
            {
                ColumnName = $"Gen {generation}";
                this.results = results;
                this.generation = generation;
                opsPerGCCount = results.Min(r => r.Value.TotalOperations);
            }

            public string ColumnName { get; private set; }
            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (results.ContainsKey(benchmark) && results[benchmark] != null)
                {
                    var result = results[benchmark];
                    if (result.GenCounts[generation] == 0)
                        return "-"; // make zero more obvious
                    return (result.GenCounts[generation] / (double)result.TotalOperations * opsPerGCCount).ToString("N2");
                }
                return "N/A";
            }
        }

        public class Stats
        {
            public long TotalOperations { get; set; }

            public int[] GenCounts = new int[4];

            public long AllocatedBytes { get; set; }
        }
    }
}
