using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Diagnosers
{
    public class ExceptionDiagnoser : IDiagnoser
    {
        public static readonly ExceptionDiagnoser Default = new ExceptionDiagnoser();

        private ExceptionDiagnoser() { }

        public IEnumerable<string> Ids => new[] { nameof(ExceptionDiagnoser) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void DisplayResults(ILogger logger) { }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            yield return new Metric(ExceptionsFrequencyMetricDescriptor.Instance, results.ExceptionFrequency);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Enumerable.Empty<ValidationError>();

        private class ExceptionsFrequencyMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Instance = new ExceptionsFrequencyMetricDescriptor();

            public string Id => "ExceptionFrequency";
            public string DisplayName => Column.Exceptions;
            public string Legend => "Exceptions thrown per single operation";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }
    }
}
