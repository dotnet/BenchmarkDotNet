using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Diagnostics
{
    public class GCDiagnoser : ETWDiagnoser, IDiagnoser, IColumnProvider
    {
        private readonly List<OutputLine> output = new List<OutputLine>();
        private readonly LogCapture logger = new LogCapture();
        private readonly Dictionary<Benchmark, Stats> results = new Dictionary<Benchmark, Stats>();
        private TraceEventSession session;
        private readonly ConcurrentDictionary<int, Stats> statsPerProcess = new ConcurrentDictionary<int, Stats>();

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
            statsPerProcess.Clear();

            var sessionName = GetSessionName("GC", benchmark, benchmark.Parameters);
            session = new TraceEventSession(sessionName);
            session.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose,
                (ulong)(ClrTraceEventParser.Keywords.GC));

            // The ETW collection thread starts receiving events immediately, but we only
            // start aggregating them after ProcessStarted is called and we know which process
            // (or processes) we should be monitoring. Communication between the benchmark thread
            // and the ETW collection thread is through the statsPerProcess concurrent dictionary
            // and through the TraceEventSession class, which is thread-safe.
            Task.Factory.StartNew(StartProcessingEvents, TaskCreationOptions.LongRunning);
        }

        public void Stop(Benchmark benchmark, BenchmarkReport report)
        {
            // ETW real-time sessions receive events with a slight delay. Typically it
            // shouldn't be more than a few seconds. This increases the likelihood that
            // all relevant events are processed by the collection thread by the time we
            // are done with the benchmark.
            Thread.Sleep(TimeSpan.FromSeconds(3));

            session.Dispose();

            var stats = ProcessEtwEvents(report.AllMeasurements.Sum(m => m.Operations));
            results.Add(benchmark, stats);
        }

        public void ProcessStarted(Process process)
        {
            ProcessIdsUsedInRuns.Add(process.Id);
            statsPerProcess.TryAdd(process.Id, new Stats());
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
                logger.WriteLine(line.Kind, line.Text);
        }

        private Stats ProcessEtwEvents(long totalOperations)
        {
            if (ProcessIdsUsedInRuns.Count > 0)
            {
                var processToReport = ProcessIdsUsedInRuns[0];
                Stats stats;
                if (statsPerProcess.TryGetValue(processToReport, out stats))
                {
                    stats.TotalOperations = totalOperations;
                    return stats;
                }
            }
            return null;
        }

        private void StartProcessingEvents()
        {
            session.Source.Clr.GCAllocationTick += gcData =>
            {
                Stats stats;
                if (statsPerProcess.TryGetValue(gcData.ProcessID, out stats))
                    stats.AllocatedBytes += gcData.AllocationAmount64;
            };

            session.Source.Clr.GCStart += gcData =>
            {
                Stats stats;
                if (statsPerProcess.TryGetValue(gcData.ProcessID, out stats))
                {
                    var genCounts = stats.GenCounts;
                    if (gcData.Depth >= 0 && gcData.Depth < genCounts.Length)
                    {
                        // BenchmarkDotNet calls GC.Collect(..) before/after benchmark runs, ignore these in our results!!!
                        if (gcData.Reason != GCReason.Induced)
                            genCounts[gcData.Depth]++;
                    }
                    else
                    {
                        logger.WriteLineError(string.Format("Error Process{0}, Unexpected GC Depth: {1}, Count: {2} -> Reason: {3}", gcData.ProcessID, gcData.Depth, gcData.Count, gcData.Reason));
                    }
                }
            };

            session.Source.Process();
        }

        public class AllocationColumn : IColumn
        {
            private Dictionary<Benchmark, Stats> results;

            public AllocationColumn(Dictionary<Benchmark, Stats> results)
            {
                this.results = results;
            }

            public string ColumnName => "Bytes Allocated/Op";
            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (results.ContainsKey(benchmark) && results[benchmark] != null)
                {
                    var result = results[benchmark];
                    // TODO scale this based on the minimum value in the column, i.e. use B/KB/MB as appropriate
                    return (result.AllocatedBytes / (double)result.TotalOperations).ToString("N2");
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
                opsPerGCCount = results.Min(r => r.Value?.TotalOperations ?? 0);
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
