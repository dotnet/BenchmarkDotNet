﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class CompositeDiagnoser : IDiagnoser
    {
        private readonly IDiagnoser[] diagnosers;

        public CompositeDiagnoser(params IDiagnoser[] diagnosers)
        {
            this.diagnosers = diagnosers.Distinct().ToArray();
        }

        public IColumnProvider GetColumnProvider() 
            => new CompositeColumnProvider(diagnosers.Select(d => d.GetColumnProvider()).ToArray());

        public void BeforeAnythingElse(DiagnoserActionParameters parameters) 
            => diagnosers.ForEach(diagnoser => diagnoser.BeforeAnythingElse(parameters));

        public void AfterSetup(DiagnoserActionParameters parameters) 
            => diagnosers.ForEach(diagnoser => diagnoser.AfterSetup(parameters));

        public void BeforeMainRun(DiagnoserActionParameters parameters) 
            => diagnosers.ForEach(diagnoser => diagnoser.BeforeMainRun(parameters));

        public void BeforeCleanup() => diagnosers.ForEach(diagnoser => diagnoser.BeforeCleanup());

        public void ProcessResults(Benchmark benchmark, BenchmarkReport report)
            => diagnosers.ForEach(diagnoser => diagnoser.ProcessResults(benchmark, report));

        public void DisplayResults(ILogger logger)
        {
            foreach (var diagnoser in diagnosers)
            {
                // TODO when Diagnosers/Diagnostis are wired up properly, instead of the Type name, 
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