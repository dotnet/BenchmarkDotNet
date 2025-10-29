using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Extensions;
using System.Collections.Generic;

namespace BenchmarkDotNet.IntegrationTests.Diagnosers
{
    public sealed class MockInProcessDiagnoser : IInProcessDiagnoser
    {
        public Dictionary<BenchmarkCase, string> Results { get; } = [];

        public IEnumerable<string> Ids => [nameof(MockInProcessDiagnoser)];

        public IEnumerable<IExporter> Exporters => [];

        public IEnumerable<IAnalyser> Analysers => [];

        public void DeserializeResults(BenchmarkCase benchmarkCase, string results) => Results.Add(benchmarkCase, results);

        public void DisplayResults(ILogger logger) => logger.WriteLine($"{nameof(MockInProcessDiagnoser)} results: [{string.Join(", ", Results.Values)}]");

        public IInProcessDiagnoserHandler? GetHandler(BenchmarkCase benchmarkCase) => new MockInProcessDiagnoserHandler();

        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];
    }

    public sealed class MockInProcessDiagnoserHandler : IInProcessDiagnoserHandler
    {
        public void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs parameters) { }

        public string SerializeResults() => "MockResult";

        public string ToSourceCode()
            => $"new {typeof(MockInProcessDiagnoserHandler).GetCorrectCSharpTypeName()}()";
    }
}