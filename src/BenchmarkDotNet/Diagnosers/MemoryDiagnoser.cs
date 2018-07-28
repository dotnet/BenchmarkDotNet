using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class MemoryDiagnoser : IDiagnoser
    {
        private const int Gen0 = 0, Gen1 = 1, Gen2 = 2;

        public static readonly MemoryDiagnoser Default = new MemoryDiagnoser();
        public const string DiagnoserId = nameof(MemoryDiagnoser); 

        private readonly Dictionary<BenchmarkCase, GcStats> results = new Dictionary<BenchmarkCase, GcStats>();

        public IEnumerable<string> Ids => new[] { DiagnoserId };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public IColumnProvider GetColumnProvider() => new SimpleColumnProvider(
            new GCCollectionColumn(results, Gen0),
            new GCCollectionColumn(results, Gen1),
            new GCCollectionColumn(results, Gen2),
            new AllocationColumn(results));

        // the following methods are left empty on purpose
        // the action takes places in other process, and the values are gathered by Engine
        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public void DisplayResults(ILogger logger) { }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead; 

        public void ProcessResults(DiagnoserResults results) 
            => this.results.Add(results.BenchmarkCase, results.GcStats);

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) 
            => Array.Empty<ValidationError>();
        
        public class AllocationColumn : IColumn
        {
            private readonly Dictionary<BenchmarkCase, GcStats> results;

            public AllocationColumn(Dictionary<BenchmarkCase, GcStats> results) => this.results = results;

            public string Id => nameof(AllocationColumn);
            public string ColumnName => "Allocated";
            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

            public bool IsAvailable(Summary summary) 
                => !RuntimeInformation.IsMono || results.Keys.Any(benchmark => !(benchmark.Job.Environment.Runtime is MonoRuntime));

            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Size;
            public string Legend => "Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)";
            public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style)
            {
                if (!results.ContainsKey(benchmarkCase) || benchmarkCase.Job.Environment.Runtime is MonoRuntime)
                    return "N/A";

                long value = results[benchmarkCase].BytesAllocatedPerOperation;
                return UnitType == UnitType.Size ? value.ToSizeStr(style.SizeUnit, 1, style.PrintUnitsInContent) : ((double)value).ToStr();
            }
        }

        public class GCCollectionColumn : IColumn
        {
            private readonly Dictionary<BenchmarkCase, GcStats> results;
            private readonly int generation;

            public GCCollectionColumn(Dictionary<BenchmarkCase, GcStats> results, int generation)
            {
                this.results = results;
                this.generation = generation;
            }

            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
            public string Id => $"{nameof(GCCollectionColumn)}{generation}";
            public string ColumnName => $"Gen {generation}";

            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Dimensionless;
            public string Legend => $"GC Generation {generation} collects per 1k Operations";
            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style) => GetValue(summary, benchmarkCase);

            public bool IsAvailable(Summary summary)
                => summary.Reports.Any(report => report.GcStats.GetCollectionsCount(generation) != 0);

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
            {
                if (results.ContainsKey(benchmarkCase))
                {
                    var gcStats = results[benchmarkCase];
                    int value = gcStats.GetCollectionsCount(generation);

                    if (value == 0)
                        return "-"; // make zero more obvious

                    return (value / (double)gcStats.TotalOperations * 1000).ToString("#0.0000", HostEnvironmentInfo.MainCultureInfo);
                }
                return "N/A";
            }
        }
    }
}