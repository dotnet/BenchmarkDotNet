using System;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
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

        public IEnumerable<string> Ids => new string[] { nameof(UnresolvedDiagnoser) };
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();
        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }
        public IEnumerable<Metric> ProcessResults(DiagnoserResults _) => Array.Empty<Metric>();

        public void DisplayResults(ILogger logger) => logger.WriteLineError(GetErrorMessage());

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => new[] { new ValidationError(false, GetErrorMessage()) };

        private string GetErrorMessage() => $@"Unable to resolve {unresolved.Name} diagnoser using dynamic assembly loading. 
            {(RuntimeInformation.IsFullFramework || RuntimeInformation.IsWindows()
                ? "Please make sure that you have installed the latest BenchmarkDotNet.Diagnostics.Windows package. " + Environment.NewLine
                    + "If you are using `dotnet build` you also need to consume one of its public types to make sure that MSBuild copies it to the output directory. "
                    + "The alternative is to use `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` in your project file."
                : $"Please make sure that it's supported on {RuntimeInformation.GetOsVersion()}")}";
    }
}