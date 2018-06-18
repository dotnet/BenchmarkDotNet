using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    [Obsolete("Please use our new BenchmarkDotNet.Diagnosers.MemoryDiagnoser", true)]
    public class MemoryDiagnoser : EtwDiagnoser<MemoryDiagnoser.Stats>, IDiagnoser
    {
        private readonly Dictionary<BenchmarkCase, Stats> results = new Dictionary<BenchmarkCase, Stats>();

        public const string DiagnoserId = nameof(MemoryDiagnoser) + "Obsolete";
        public IEnumerable<string> Ids => new[] { DiagnoserId };

        public IColumnProvider GetColumnProvider() => new SimpleColumnProvider(
            new GCCollectionColumn(results, 0),
            new GCCollectionColumn(results, 1),
            new GCCollectionColumn(results, 2),
            new AllocationColumn(results));

        protected override ulong EventType => (ulong)ClrTraceEventParser.Keywords.GC;

        protected override string SessionNamePrefix => "GC";

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal == HostSignal.BeforeMainRun)
                Start(parameters);
            else if(signal == HostSignal.AfterMainRun)
                Stop();
        }

        public void ProcessResults(DiagnoserResults results)
        {
            var stats = ProcessEtwEvents(results.BenchmarkCase, results.TotalOperations);
            this.results.Add(results.BenchmarkCase, stats);
        }

        public void DisplayResults(ILogger logger) { }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Enumerable.Empty<ValidationError>();

        private Stats ProcessEtwEvents(BenchmarkCase benchmarkCase, long totalOperations)
        {
            if (BenchmarkToProcess.Count > 0)
            {
                var processToReport = BenchmarkToProcess[benchmarkCase];
                if (StatsPerProcess.TryGetValue(processToReport, out Stats stats))
                {
                    stats.TotalOperations = totalOperations;
                    return stats;
                }
            }
            return null;
        }

        protected override void AttachToEvents(TraceEventSession session, BenchmarkCase benchmarkCase)
        {
            session.Source.Clr.GCAllocationTick += gcData =>
            {
                if (StatsPerProcess.TryGetValue(gcData.ProcessID, out Stats stats))
                    stats.AllocatedBytes += gcData.AllocationAmount64;
            };

            session.Source.Clr.GCStart += gcData =>
            {
                if (StatsPerProcess.TryGetValue(gcData.ProcessID, out Stats stats))
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
            private Dictionary<BenchmarkCase, Stats> results;

            public AllocationColumn(Dictionary<BenchmarkCase, Stats> results)
            {
                this.results = results;
            }

            public string Id => nameof(AllocationColumn);
            public string ColumnName => "Bytes Allocated/Op";
            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Size;
            public string Legend => "";
            public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style)
            {
                if (!results.ContainsKey(benchmarkCase) || results[benchmarkCase] == null)
                    return "N/A";

                var value = results[benchmarkCase].AllocatedBytes / (double)results[benchmarkCase].TotalOperations;
                return UnitType == UnitType.Size ? ((long)value).ToSizeStr(style.SizeUnit, 1, style.PrintUnitsInContent) : value.ToStr();
            }
        }

        public class GCCollectionColumn : IColumn
        {
            private Dictionary<BenchmarkCase, Stats> results;
            private int generation;
            // TODO also need to find a sensible way of including this in the column name?
            private long opsPerGCCount;

            public GCCollectionColumn(Dictionary<BenchmarkCase, Stats> results, int generation)
            {
                ColumnName = $"Gen {generation}";
                this.results = results;
                this.generation = generation;
                opsPerGCCount = results.Min(r => r.Value?.TotalOperations ?? 0);
            }

            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
            public string Id => nameof(GCCollectionColumn);
            public string ColumnName { get; private set; }
            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Dimensionless;
            public string Legend => "";
            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style) => GetValue(summary, benchmarkCase);

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
            {
                if (results.ContainsKey(benchmarkCase) && results[benchmarkCase] != null)
                {
                    var result = results[benchmarkCase];
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