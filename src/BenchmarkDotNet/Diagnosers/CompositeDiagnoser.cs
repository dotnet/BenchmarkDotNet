﻿using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class CompositeDiagnoser : IDiagnoser
    {
        private readonly IDiagnoser[] diagnosers;

        public CompositeDiagnoser(params IDiagnoser[] diagnosers) => this.diagnosers = diagnosers.Distinct().ToArray();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => throw new InvalidOperationException("Should never be called for Composite Diagnoser");

        public IEnumerable<string> Ids => diagnosers.SelectMany(d => d.Ids);

        public IEnumerable<IExporter> Exporters 
            => diagnosers.SelectMany(diagnoser => diagnoser.Exporters);

        public IEnumerable<IAnalyser> Analysers
            => diagnosers.SelectMany(diagnoser => diagnoser.Analysers);

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
            => diagnosers.ForEach(diagnoser => diagnoser.Handle(signal, parameters));

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
            => diagnosers.SelectMany(diagnoser => diagnoser.ProcessResults(results));

        public void DisplayResults(ILogger logger)
        {
            foreach (var diagnoser in diagnosers)
            {
                // TODO when Diagnosers/Diagnostics are wired up properly, instead of the Type name, 
                // print the name used on the cmd line, i.e. -d=<NAME>
                logger.WriteLineHeader($"// * Diagnostic Output - {diagnoser.GetType().Name} *");
                diagnoser.DisplayResults(logger);
                logger.WriteLine();
            }
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) 
            => diagnosers.SelectMany(diagnoser => diagnoser.Validate(validationParameters));
    }
}