using System;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class UnresolvedDiagnoser : IDiagnoser
    {
        private readonly Type unresolved;

        public UnresolvedDiagnoser(Type unresolved) => this.unresolved = unresolved;

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.None;

        public IEnumerable<string> Ids => Array.Empty<string>();
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }
        public IEnumerable<Metric> ProcessResults(DiagnoserResults _) => Array.Empty<Metric>();

        public void DisplayResults(ILogger logger) => logger.WriteLineError(GetErrorMessage());

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => new[] { new ValidationError(false, GetErrorMessage()) };

        private string GetErrorMessage() => $@"Unable to resolve {unresolved.Name} diagnoser. 
            {(RuntimeInformation.IsFullFramework || RuntimeInformation.IsWindows()
                ? "Please make sure that you have installed the latest BenchmarkDotNet.Diagnostics.Windows package and consumed one of its public types to make sure that MSBuild copies it to the output directory."
                : $"Please make sure that it's supported on {RuntimeInformation.GetOsVersion()}")}";
    }
}