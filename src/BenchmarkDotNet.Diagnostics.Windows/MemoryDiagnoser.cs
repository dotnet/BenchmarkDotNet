using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    [Obsolete("Please use our new BenchmarkDotNet.Diagnosers.MemoryDiagnoser", true)]
    public class MemoryDiagnoser : EtwDiagnoser<MemoryDiagnoser.Stats>, IDiagnoser
    {
        private readonly Dictionary<Benchmark, Stats> results = new Dictionary<Benchmark, Stats>();

        public IColumnProvider GetColumnProvider() => new SimpleColumnProvider(
            new GCCollectionColumn(results, 0),
            new GCCollectionColumn(results, 1),
            new GCCollectionColumn(results, 2),
            new AllocationColumn(results));

        protected override ulong EventType => (ulong)ClrTraceEventParser.Keywords.GC;

        protected override string SessionNamePrefix => "GC";

        public void BeforeAnythingElse(Process process, Benchmark benchmark) { }

        public void AfterSetup(Process process, Benchmark benchmark) { }

        public void BeforeMainRun(Process process, Benchmark benchmark) => Start(process, benchmark);

        public void BeforeCleanup() => Stop();

        public void ProcessResults(Benchmark benchmark, BenchmarkReport report)
        {
            var stats = ProcessEtwEvents(benchmark, report.AllMeasurements.Sum(m => m.Operations));
            results.Add(benchmark, stats);
        }

        public void DisplayResults(ILogger logger) { }

        private Stats ProcessEtwEvents(Benchmark benchmark, long totalOperations)
        {
            if (BenchmarkToProcess.Count > 0)
            {
                var processToReport = BenchmarkToProcess[benchmark];
                Stats stats;
                if (StatsPerProcess.TryGetValue(processToReport, out stats))
                {
                    stats.TotalOperations = totalOperations;
                    return stats;
                }
            }
            return null;
        }

        protected override void AttachToEvents(TraceEventSession session, Benchmark benchmark)
        {
            session.Source.Clr.GCAllocationTick += gcData =>
            {
                Stats stats;
                if (StatsPerProcess.TryGetValue(gcData.ProcessID, out stats))
                    stats.AllocatedBytes += gcData.AllocationAmount64;
            };

            session.Source.Clr.GCStart += gcData =>
            {
                Stats stats;
                if (StatsPerProcess.TryGetValue(gcData.ProcessID, out stats))
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
                        Logger.WriteLineError(string.Format("Error Process{0}, Unexpected GC Depth: {1}, Count: {2} -> Reason: {3}", gcData.ProcessID,
                            gcData.Depth, gcData.Count, gcData.Reason));
                    }
                }
            };
        }

        public class AllocationColumn : IColumn
        {
            private Dictionary<Benchmark, Stats> results;

            public AllocationColumn(Dictionary<Benchmark, Stats> results)
            {
                this.results = results;
            }

            public string Id => nameof(AllocationColumn);
            public string ColumnName => "Bytes Allocated/Op";
            public bool IsDefault(Summary summary, Benchmark benchmark) => false;

            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (results.ContainsKey(benchmark) && results[benchmark] != null)
                {
                    var result = results[benchmark];
                    // TODO scale this based on the minimum value in the column, i.e. use B/KB/MB as appropriate
                    return (result.AllocatedBytes / (double) result.TotalOperations).ToString("N2");
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

            public bool IsDefault(Summary summary, Benchmark benchmark) => false;
            public string Id => nameof(GCCollectionColumn);
            public string ColumnName { get; private set; }
            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (results.ContainsKey(benchmark) && results[benchmark] != null)
                {
                    var result = results[benchmark];
                    if (result.GenCounts[generation] == 0)
                        return "-"; // make zero more obvious
                    return (result.GenCounts[generation] / (double) result.TotalOperations * opsPerGCCount).ToString("N2", HostEnvironmentInfo.MainCultureInfo);
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