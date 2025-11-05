using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System.Collections.Generic;
using System;

namespace BenchmarkDotNet.IntegrationTests.Diagnosers;

public sealed class MockInProcessDiagnoser : IInProcessDiagnoser
{
    public Dictionary<BenchmarkCase, string> Results { get; } = [];

    public IEnumerable<string> Ids => [nameof(MockInProcessDiagnoser)];

    public IEnumerable<IExporter> Exporters => [];

    public IEnumerable<IAnalyser> Analysers => [];

    public void DisplayResults(ILogger logger) => logger.WriteLine($"{nameof(MockInProcessDiagnoser)} results: [{string.Join(", ", Results.Values)}]");

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    public (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => (typeof(MockInProcessDiagnoserHandler), null);

    public IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase)
        => new MockInProcessDiagnoserHandler();

    public void DeserializeResults(BenchmarkCase benchmarkCase, string results) => Results.Add(benchmarkCase, results);
}

public sealed class MockInProcessDiagnoserHandler : IInProcessDiagnoserHandler
{
    public void Initialize(string? serializedConfig) { }

    public void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args) { }

    public string SerializeResults() => "MockResult";
}

public sealed class MockInProcessDiagnoserNoOverhead : IInProcessDiagnoser
{
    public Dictionary<BenchmarkCase, string> Results { get; } = [];

    public IEnumerable<string> Ids => [nameof(MockInProcessDiagnoserNoOverhead)];

    public IEnumerable<IExporter> Exporters => [];

    public IEnumerable<IAnalyser> Analysers => [];

    public void DisplayResults(ILogger logger) => logger.WriteLine($"{nameof(MockInProcessDiagnoserNoOverhead)} results: [{string.Join(", ", Results.Values)}]");

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    public (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => (typeof(MockInProcessDiagnoserNoOverheadHandler), null);

    public IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase)
        => new MockInProcessDiagnoserNoOverheadHandler();

    public void DeserializeResults(BenchmarkCase benchmarkCase, string results) => Results.Add(benchmarkCase, results);
}

public sealed class MockInProcessDiagnoserNoOverheadHandler : IInProcessDiagnoserHandler
{
    public void Initialize(string? serializedConfig) { }

    public void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args) { }

    public string SerializeResults() => "NoOverheadResult";
}

public sealed class MockInProcessDiagnoserExtraRun : IInProcessDiagnoser
{
    public Dictionary<BenchmarkCase, string> Results { get; } = [];

    public IEnumerable<string> Ids => [nameof(MockInProcessDiagnoserExtraRun)];

    public IEnumerable<IExporter> Exporters => [];

    public IEnumerable<IAnalyser> Analysers => [];

    public void DisplayResults(ILogger logger) => logger.WriteLine($"{nameof(MockInProcessDiagnoserExtraRun)} results: [{string.Join(", ", Results.Values)}]");

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.ExtraRun;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    public (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => (typeof(MockInProcessDiagnoserExtraRunHandler), null);

    public IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase)
        => new MockInProcessDiagnoserExtraRunHandler();

    public void DeserializeResults(BenchmarkCase benchmarkCase, string results) => Results.Add(benchmarkCase, results);
}

public sealed class MockInProcessDiagnoserExtraRunHandler : IInProcessDiagnoserHandler
{
    public void Initialize(string? serializedConfig) { }

    public void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args) { }

    public string SerializeResults() => "ExtraRunResult";
}

public sealed class MockInProcessDiagnoserNone : IInProcessDiagnoser
{
    public Dictionary<BenchmarkCase, string> Results { get; } = [];

    public IEnumerable<string> Ids => [nameof(MockInProcessDiagnoserNone)];

    public IEnumerable<IExporter> Exporters => [];

    public IEnumerable<IAnalyser> Analysers => [];

    public void DisplayResults(ILogger logger) => logger.WriteLine($"{nameof(MockInProcessDiagnoserNone)} results: [{string.Join(", ", Results.Values)}]");

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.None;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    public (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => default; // Returns default when RunMode is None

    public IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase)
        => null; // Returns null when RunMode is None

    public void DeserializeResults(BenchmarkCase benchmarkCase, string results) => Results.Add(benchmarkCase, results);
}