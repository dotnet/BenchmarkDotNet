using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Linq;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Diagnosers
{
    public class MemoryDiagnoser : IDiagnoser
    {
        public static readonly MemoryDiagnoser Default = new MemoryDiagnoser();

        private readonly Dictionary<Benchmark, GcStats> results = new Dictionary<Benchmark, GcStats>();

        public IColumnProvider GetColumnProvider() => new SimpleColumnProvider(
            new GCCollectionColumn(results, 0),
            new GCCollectionColumn(results, 1),
            new GCCollectionColumn(results, 2),
            new AllocationColumn(results));

        // the methods are left empty on purpose
        // the action takes places in other process, and the values are gathered by Engine
        public void BeforeAnythingElse(Process process, Benchmark benchmark) { }
        public void AfterSetup(Process process, Benchmark benchmark) { }
        public void BeforeCleanup() { }

        public void ProcessResults(Benchmark benchmark, BenchmarkReport report)
        {
            results.Add(benchmark, report.GcStats);
        }

        public void DisplayResults(ILogger logger) { }

        public class AllocationColumn : IColumn
        {
            private readonly Dictionary<Benchmark, GcStats> results;

            public AllocationColumn(Dictionary<Benchmark, GcStats> results)
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
#if !CORE
                if (results.ContainsKey(benchmark))
                {
                    var result = results[benchmark];
                    // TODO scale this based on the minimum value in the column, i.e. use B/KB/MB as appropriate
                    return (result.AllocatedBytes / result.TotalOperations).ToString("N2", HostEnvironmentInfo.MainCultureInfo);
                }
                return "N/A";
#else
                return "?";
#endif
            }
        }

        public class GCCollectionColumn : IColumn
        {
            private Dictionary<Benchmark, GcStats> results;
            private int generation;
            // TODO also need to find a sensible way of including this in the column name?
            private long opsPerGCCount;

            public GCCollectionColumn(Dictionary<Benchmark, GcStats> results, int generation)
            {
                ColumnName = $"Gen {generation}";
                this.results = results;
                this.generation = generation;
                opsPerGCCount = results.Any() ? results.Min(r => r.Value.TotalOperations) : 1;
            }

            public bool IsDefault(Summary summary, Benchmark benchmark) => true;
            public string Id => $"{nameof(GCCollectionColumn)}{generation}";
            public string ColumnName { get; }
            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (results.ContainsKey(benchmark))
                {
                    var result = results[benchmark];
                    var value = generation == 0 ? result.Gen0Collections : 
                                generation == 1 ? result.Gen1Collections : result.Gen2Collections;

                    if (value == 0)
                        return "-"; // make zero more obvious
                    return (value / (double)result.TotalOperations * opsPerGCCount).ToString("N2", HostEnvironmentInfo.MainCultureInfo);
                }
                return "N/A";
            }
        }
    }
}