using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class CompositeDiagnoser : IDiagnoser
    {
        private readonly ImmutableHashSet<IDiagnoser> diagnosers;

        public CompositeDiagnoser(ImmutableHashSet<IDiagnoser> diagnosers)
            => this.diagnosers = diagnosers;

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
            => throw new InvalidOperationException("Should never be called for Composite Diagnoser");

        public IEnumerable<string> Ids
            => diagnosers.SelectMany(d => d.Ids);

        public IEnumerable<IExporter> Exporters
            => diagnosers.SelectMany(diagnoser => diagnoser.Exporters);

        public IEnumerable<IAnalyser> Analysers
            => diagnosers.SelectMany(diagnoser => diagnoser.Analysers);

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            foreach (var diagnoser in diagnosers)
                diagnoser.Handle(signal, parameters);
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
            => diagnosers.SelectMany(diagnoser => diagnoser.ProcessResults(results));

        public void DisplayResults(ILogger logger)
        {
            foreach (var diagnoser in diagnosers)
            {
                logger.WriteLineHeader($"// * Diagnostic Output - {diagnoser.Ids.Single()} *");
                diagnoser.DisplayResults(logger);
                logger.WriteLine();
            }
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => diagnosers.SelectMany(diagnoser => diagnoser.Validate(validationParameters));
    }
}