﻿using System;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class UnresolvedDiagnoser : IDiagnoser
    {
        private readonly Type unresolved;

        public UnresolvedDiagnoser(Type unresolved) => this.unresolved = unresolved;

        public RunMode GetRunMode(Benchmark benchmark) => RunMode.None;

        public IEnumerable<string> Ids => Array.Empty<string>();
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;
        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }
        public void ProcessResults(DiagnoserResults results) { }

        public void DisplayResults(ILogger logger) => logger.WriteLineError(GetErrorMessage());

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => new[] { new ValidationError(false, GetErrorMessage()) };

        private string GetErrorMessage() => $@"Unable to resolve {unresolved.Name} diagnoser. 
            {(RuntimeInformation.IsFullFramework || RuntimeInformation.IsWindows()
                ? "Please make sure that you have installed the latest BenchmarkDotNet.Diagnostics.Windows package." 
                : $"Please make sure that it's supported on {RuntimeInformation.GetOsVersion()}")}";
    }
}