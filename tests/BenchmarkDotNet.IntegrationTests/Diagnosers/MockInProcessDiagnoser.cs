using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System.Collections.Generic;

namespace BenchmarkDotNet.IntegrationTests.Diagnosers;

public abstract class BaseMockInProcessDiagnoser(RunMode runMode) : IInProcessDiagnoser
{
    public static Queue<string> s_completedResults = new();

    public Dictionary<BenchmarkCase, string> Results { get; } = [];

    public RunMode RunMode { get; } = runMode;
    public string ExpectedResult => $"MockResult-{RunMode}";

    public IEnumerable<string> Ids => [GetType().Name];

    public IEnumerable<IExporter> Exporters => [];

    public IEnumerable<IAnalyser> Analysers => [];

    public void DisplayResults(ILogger logger) => logger.WriteLine($"{GetType().Name} results: [{string.Join(", ", Results.Values)}]");

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    InProcessDiagnoserHandlerData IInProcessDiagnoser.GetHandlerData(BenchmarkCase benchmarkCase)
        => RunMode == RunMode.None
            ? default
            : new(typeof(MockInProcessDiagnoserHandler), ExpectedResult);

    public void DeserializeResults(BenchmarkCase benchmarkCase, string results)
    {
        Results.Add(benchmarkCase, results);
        s_completedResults.Enqueue(results);
    }
}

public sealed class MockInProcessDiagnoserHandler : IInProcessDiagnoserHandler
{
    private string _result;

    public void Initialize(string? serializedConfig)
    {
        _result = serializedConfig ?? string.Empty;
    }

    public void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args) { }

    public string SerializeResults() => _result;
}

// Diagnosers are made unique per-type rather than per-instance, so we have to create separate types to test multiple.
public sealed class MockInProcessDiagnoser1(RunMode runMode) : BaseMockInProcessDiagnoser(runMode) { }
public sealed class MockInProcessDiagnoser2(RunMode runMode) : BaseMockInProcessDiagnoser(runMode) { }
public sealed class MockInProcessDiagnoser3(RunMode runMode) : BaseMockInProcessDiagnoser(runMode) { }