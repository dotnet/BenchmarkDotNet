using System;
using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public class UnresolvedDiagnoser : IDiagnoser
    {
        private const string ErrorMessage =
#if CLASSIC
            "Please make sure that you have installed the latest BenchmarkDotNet.Diagnostics.Windows package.";
#else
            "To use the classic Windows diagnosers for .NET Core you need to run the benchmarks for desktop .NET. More info: http://adamsitnik.com/Hardware-Counters-Diagnoser/#how-to-get-it-running-for-net-coremono-on-windows";
#endif

        private readonly Type unresolved;

        public UnresolvedDiagnoser(Type unresolved)
        {
            this.unresolved = unresolved;
        }

        public RunMode GetRunMode(Benchmark benchmark) => RunMode.None;

        public IEnumerable<string> Ids => Array.Empty<string>();
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        public IColumnProvider GetColumnProvider() => EmptyColumnProvider.Instance;

        public void BeforeAnythingElse(DiagnoserActionParameters parameters) { }
        public void AfterGlobalSetup(DiagnoserActionParameters parameters) { }
        public void BeforeMainRun(DiagnoserActionParameters parameters) { }
        public void BeforeGlobalCleanup(DiagnoserActionParameters parameters) { }
        public void ProcessResults(Benchmark benchmark, BenchmarkReport report) { }

        public void DisplayResults(ILogger logger) => logger.WriteLineError(GetErrorMessage());

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => new[] { new ValidationError(false, GetErrorMessage()) };

        private string GetErrorMessage() => $"Unable to resolve {unresolved.Name} diagnoser. {ErrorMessage}";
    }
}