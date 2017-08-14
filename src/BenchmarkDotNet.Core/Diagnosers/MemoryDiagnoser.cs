using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class MemoryDiagnoser : IDiagnoser
    {
        private const int Gen0 = 0, Gen1 = 1, Gen2 = 2;

        public static readonly MemoryDiagnoser Default = new MemoryDiagnoser();
        public const string DiagnoserId = nameof(MemoryDiagnoser); 

        private readonly Dictionary<Benchmark, GcStats> results = new Dictionary<Benchmark, GcStats>();

        public RunMode GetRunMode(Benchmark benchmark) => RunMode.ExtraRun;

        public IEnumerable<string> Ids => new[] { DiagnoserId };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IColumnProvider GetColumnProvider() => new SimpleColumnProvider(
            new GCCollectionColumn(results, Gen0),
            new GCCollectionColumn(results, Gen1),
            new GCCollectionColumn(results, Gen2),
            new AllocationColumn(results));

        // the following methods are left empty on purpose
        // the action takes places in other process, and the values are gathered by Engine
        public void BeforeAnythingElse(DiagnoserActionParameters _) { }
        public void AfterGlobalSetup(DiagnoserActionParameters _) { }
        public void BeforeMainRun(DiagnoserActionParameters _) { }
        public void BeforeGlobalCleanup(DiagnoserActionParameters parameters) { }

        public void DisplayResults(ILogger logger) { }

        public void ProcessResults(Benchmark benchmark, BenchmarkReport report) 
            => results.Add(benchmark, report.GcStats);

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Enumerable.Empty<ValidationError>();
        
        public class AllocationColumn : IColumn
        {
            private readonly Dictionary<Benchmark, GcStats> results;

            public AllocationColumn(Dictionary<Benchmark, GcStats> results)
            {
                this.results = results;
            }

            public string Id => nameof(AllocationColumn);
            public string ColumnName => "Allocated";
            public bool IsDefault(Summary summary, Benchmark benchmark) => false;

            public bool IsAvailable(Summary summary) 
                => !RuntimeInformation.IsMono() || results.Keys.Any(benchmark => !(benchmark.Job.Env.Runtime is MonoRuntime));

            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Size;
            public string Legend => "Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)";
            public string GetValue(Summary summary, Benchmark benchmark) => GetValue(summary, benchmark, SummaryStyle.Default);

            public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style)
            {
                if (!results.ContainsKey(benchmark) || benchmark.Job.Env.Runtime is MonoRuntime)
                    return "N/A";

                var value = results[benchmark].BytesAllocatedPerOperation;
                return UnitType == UnitType.Size ? value.ToSizeStr(style.SizeUnit, 1, style.PrintUnitsInContent) : ((double)value).ToStr();
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

            public bool IsDefault(Summary summary, Benchmark benchmark) => false;
            public string Id => $"{nameof(GCCollectionColumn)}{generation}";
            public string ColumnName => $"Gen {generation}";

            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Dimensionless;
            public string Legend => $"GC Generation {generation} collects per 1k Operations";
            public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);

            public bool IsAvailable(Summary summary)
                => summary.Reports.Any(report => report.GcStats.GetCollectionsCount(generation) != 0);

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (results.ContainsKey(benchmark))
                {
                    var gcStats = results[benchmark];
                    var value = gcStats.GetCollectionsCount(generation);

                    if (value == 0)
                        return "-"; // make zero more obvious

                    return ((value / (double)gcStats.TotalOperations) * 1000).ToString("#0.0000", HostEnvironmentInfo.MainCultureInfo);
                }
                return "N/A";
            }
        }
    }
}