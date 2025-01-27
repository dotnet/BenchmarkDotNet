using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
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
        public static readonly ExceptionDiagnoser Default = new ExceptionDiagnoser(new ExceptionDiagnoserConfig(displayExceptionsIfZeroValue: true));

        public ExceptionDiagnoser(ExceptionDiagnoserConfig config) => Config = config;

        public ExceptionDiagnoserConfig Config { get; }

        public IEnumerable<string> Ids => new[] { nameof(ExceptionDiagnoser) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void DisplayResults(ILogger logger) { }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            yield return new Metric(new ExceptionsFrequencyMetricDescriptor(Config), results.ExceptionFrequency);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Enumerable.Empty<ValidationError>();

        internal class ExceptionsFrequencyMetricDescriptor : IMetricDescriptor
        {
            public ExceptionDiagnoserConfig Config { get; }
            public ExceptionsFrequencyMetricDescriptor(ExceptionDiagnoserConfig config = null)
            {
                Config = config;
            }

            public string Id => "ExceptionFrequency";
            public string DisplayName => Column.Exceptions;
            public string Legend => "Exceptions thrown per single operation";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric)
            {
                if (Config == null)
                    return metric.Value > 0;
                else
                    return Config.DisplayExceptionsIfZeroValue || metric.Value > 0;
            }
        }
    }
}