using System;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class MemoryDiagnoser : IDiagnoser
    {
        private const string DiagnoserId = nameof(MemoryDiagnoser);

        public static readonly MemoryDiagnoser Default = new MemoryDiagnoser(new MemoryDiagnoserConfig(displayGenColumns: true));

        public MemoryDiagnoser(MemoryDiagnoserConfig config) => Config = config;

        public MemoryDiagnoserConfig Config { get; }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public IEnumerable<string> Ids => new[] { DiagnoserId };
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        public void DisplayResults(ILogger logger) { }
        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Array.Empty<ValidationError>();

        // the action takes places in other process, and the values are gathered by Engine
        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults diagnoserResults)
        {
            if (Config.DisplayGenColumns)
            {
                yield return new Metric(GarbageCollectionsMetricDescriptor.Gen0, diagnoserResults.GcStats.Gen0Collections / (double) diagnoserResults.GcStats.TotalOperations * 1000);
                yield return new Metric(GarbageCollectionsMetricDescriptor.Gen1, diagnoserResults.GcStats.Gen1Collections / (double) diagnoserResults.GcStats.TotalOperations * 1000);
                yield return new Metric(GarbageCollectionsMetricDescriptor.Gen2, diagnoserResults.GcStats.Gen2Collections / (double) diagnoserResults.GcStats.TotalOperations * 1000);
            }

            yield return new Metric(AllocatedMemoryMetricDescriptor.Instance, diagnoserResults.GcStats.GetBytesAllocatedPerOperation(diagnoserResults.BenchmarkCase) ?? double.NaN);
        }

        private class GarbageCollectionsMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Gen0 = new GarbageCollectionsMetricDescriptor(0, Column.Gen0);
            internal static readonly IMetricDescriptor Gen1 = new GarbageCollectionsMetricDescriptor(1, Column.Gen1);
            internal static readonly IMetricDescriptor Gen2 = new GarbageCollectionsMetricDescriptor(2, Column.Gen2);

            private GarbageCollectionsMetricDescriptor(int generationId, string columnName)
            {
                Id = $"Gen{generationId}Collects";
                DisplayName = columnName;
                Legend = $"GC Generation {generationId} collects per 1000 operations";
                PriorityInCategory = generationId;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Legend { get; }
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory { get; }
            public bool GetIsAvailable(Metric metric) => metric.Value > 0;
        }
    }
}