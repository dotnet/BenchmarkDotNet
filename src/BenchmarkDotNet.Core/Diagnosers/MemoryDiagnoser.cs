using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Portability;

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

        // the following methods are left empty on purpose
        // the action takes places in other process, and the values are gathered by Engine
        public void BeforeAnythingElse(Process process, Benchmark benchmark) { }
        public void AfterSetup(Process process, Benchmark benchmark) { }
        public void BeforeCleanup() { }

        public void DisplayResults(ILogger logger) { } // no custom output

        public void ProcessResults(Benchmark benchmark, BenchmarkReport report) => results.Add(benchmark, report.GcStats);

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
#if CORE
                return "?";
#else
                if (RuntimeInformation.IsMono())
                    return "?";
                if (!results.ContainsKey(benchmark))
                    return "N/A";

                return results[benchmark].BytesAllocatedPerOperation.ToString("N0", HostEnvironmentInfo.MainCultureInfo);
#endif
            }
        }

        public class GCCollectionColumn : IColumn
        {
            private readonly Dictionary<Benchmark, GcStats> results;
            private readonly int generation;

            public GCCollectionColumn(Dictionary<Benchmark, GcStats> results, int generation)
            {
                this.results = results;
                this.generation = generation;
            }

            public bool IsDefault(Summary summary, Benchmark benchmark) => true;
            public string Id => $"{nameof(GCCollectionColumn)}{generation}";
            public string ColumnName => $"Gen {generation}/op";
            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (results.ContainsKey(benchmark))
                {
                    var gcStats = results[benchmark];
                    var value = generation == 0 ? gcStats.Gen0Collections : 
                                generation == 1 ? gcStats.Gen1Collections : gcStats.Gen2Collections;

                    if (value == 0)
                        return "-"; // make zero more obvious

                    return (value / (double)gcStats.TotalOperations).ToString("N6", HostEnvironmentInfo.MainCultureInfo);
                }
                return "N/A";
            }
        }
    }
}