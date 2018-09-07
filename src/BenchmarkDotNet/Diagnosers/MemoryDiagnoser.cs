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
            yield return new Metric("Gen 0/1k Op", diagnoserResults.GcStats.Gen0Collections / (double)diagnoserResults.GcStats.TotalOperations * 1000, "GC Generation 0 collects per 1k Operations", "#0.0000");
            yield return new Metric("Gen 1/1k Op", diagnoserResults.GcStats.Gen1Collections / (double)diagnoserResults.GcStats.TotalOperations * 1000, "GC Generation 1 collects per 1k Operations", "#0.0000");
            yield return new Metric("Gen 2/1k Op", diagnoserResults.GcStats.Gen2Collections / (double)diagnoserResults.GcStats.TotalOperations * 1000, "GC Generation 2 collects per 1k Operations", "#0.0000");
            yield return new Metric("Allocated Memory/Op", diagnoserResults.GcStats.BytesAllocatedPerOperation, "Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)", SizeUnit.B.Name, UnitType.Size);
        }
    }
}