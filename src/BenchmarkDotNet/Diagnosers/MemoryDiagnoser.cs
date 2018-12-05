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
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public class MemoryDiagnoser : IDiagnoser
    {
        private const string DiagnoserId = nameof(MemoryDiagnoser);
        
        public static readonly MemoryDiagnoser Default = new MemoryDiagnoser();
        
        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public IEnumerable<string> Ids => new[] { DiagnoserId };
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        public void DisplayResults(ILogger logger) { }
        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Array.Empty<ValidationError>();
        
        // the following methods are left empty on purpose
        // the action takes places in other process, and the values are gathered by Engine
        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults diagnoserResults)
        {
            yield return new Metric(GarbageCollectionsMetricDescriptor.Gen0, diagnoserResults.GcStats.Gen0Collections / (double)diagnoserResults.GcStats.TotalOperations * 1000);
            yield return new Metric(GarbageCollectionsMetricDescriptor.Gen1, diagnoserResults.GcStats.Gen1Collections / (double)diagnoserResults.GcStats.TotalOperations * 1000);
            yield return new Metric(GarbageCollectionsMetricDescriptor.Gen2, diagnoserResults.GcStats.Gen2Collections / (double)diagnoserResults.GcStats.TotalOperations * 1000);
            yield return new Metric(AllocatedMemoryMetricDescriptor.Instance, diagnoserResults.GcStats.BytesAllocatedPerOperation);
        }

        private class AllocatedMemoryMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Instance = new AllocatedMemoryMetricDescriptor();
            
            public string Id => "Allocated Memory";
            public string DisplayName => "Allocated Memory/Op";
            public string Legend => "Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)";
            public string NumberFormat => "N0";
            public UnitType UnitType => UnitType.Size;
            public string Unit => SizeUnit.B.Name;
            public bool TheGreaterTheBetter => false;
        }

        private class GarbageCollectionsMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Gen0 = new GarbageCollectionsMetricDescriptor(0);
            internal static readonly IMetricDescriptor Gen1 = new GarbageCollectionsMetricDescriptor(1);
            internal static readonly IMetricDescriptor Gen2 = new GarbageCollectionsMetricDescriptor(2);

            private GarbageCollectionsMetricDescriptor(int generationId)
            {
                Id = $"Gen{generationId}Collects";
                DisplayName = $"Gen {generationId}/1k Op";
                Legend = $"GC Generation {generationId} collects per 1k Operations";
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Legend { get; }
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
        }
    }
}