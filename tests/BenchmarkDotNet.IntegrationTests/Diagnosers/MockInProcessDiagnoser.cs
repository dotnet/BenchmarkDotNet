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

public abstract class BaseMockInProcessDiagnoser : IInProcessDiagnoser
{
    public Dictionary<BenchmarkCase, string> Results { get; } = [];

    public abstract string DiagnoserName { get; }
    public abstract RunMode DiagnoserRunMode { get; }
    public abstract Type HandlerType { get; }
    public abstract string ExpectedResult { get; }

    public IEnumerable<string> Ids => [DiagnoserName];

    public IEnumerable<IExporter> Exporters => [];

    public IEnumerable<IAnalyser> Analysers => [];

    public void DisplayResults(ILogger logger) => logger.WriteLine($"{DiagnoserName} results: [{string.Join(", ", Results.Values)}]");

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => DiagnoserRunMode;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    public virtual (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => (HandlerType, null);

    public virtual IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase)
        => (IInProcessDiagnoserHandler)Activator.CreateInstance(HandlerType, ExpectedResult);

    public void DeserializeResults(BenchmarkCase benchmarkCase, string results) => Results.Add(benchmarkCase, results);
}

public abstract class BaseMockInProcessDiagnoserHandler : IInProcessDiagnoserHandler
{
    private readonly string _result;

    protected BaseMockInProcessDiagnoserHandler(string result) => _result = result;

    public void Initialize(string? serializedConfig) { }

    public void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args) { }

    public string SerializeResults() => _result;
}

public sealed class MockInProcessDiagnoser : BaseMockInProcessDiagnoser
{
    public override string DiagnoserName => nameof(MockInProcessDiagnoser);
    public override RunMode DiagnoserRunMode => RunMode.NoOverhead;
    public override Type HandlerType => typeof(MockInProcessDiagnoserHandler);
    public override string ExpectedResult => "MockResult";
}

public sealed class MockInProcessDiagnoserHandler : BaseMockInProcessDiagnoserHandler
{
    public MockInProcessDiagnoserHandler(string result) : base(result) { }
}

public sealed class MockInProcessDiagnoserNoOverhead : BaseMockInProcessDiagnoser
{
    public override string DiagnoserName => nameof(MockInProcessDiagnoserNoOverhead);
    public override RunMode DiagnoserRunMode => RunMode.NoOverhead;
    public override Type HandlerType => typeof(MockInProcessDiagnoserNoOverheadHandler);
    public override string ExpectedResult => "NoOverheadResult";
}

public sealed class MockInProcessDiagnoserNoOverheadHandler : BaseMockInProcessDiagnoserHandler
{
    public MockInProcessDiagnoserNoOverheadHandler(string result) : base(result) { }
}

public sealed class MockInProcessDiagnoserExtraRun : BaseMockInProcessDiagnoser
{
    public override string DiagnoserName => nameof(MockInProcessDiagnoserExtraRun);
    public override RunMode DiagnoserRunMode => RunMode.ExtraRun;
    public override Type HandlerType => typeof(MockInProcessDiagnoserExtraRunHandler);
    public override string ExpectedResult => "ExtraRunResult";
}

public sealed class MockInProcessDiagnoserExtraRunHandler : BaseMockInProcessDiagnoserHandler
{
    public MockInProcessDiagnoserExtraRunHandler(string result) : base(result) { }
}

public sealed class MockInProcessDiagnoserNone : BaseMockInProcessDiagnoser
{
    public override string DiagnoserName => nameof(MockInProcessDiagnoserNone);
    public override RunMode DiagnoserRunMode => RunMode.None;
    public override Type HandlerType => typeof(MockInProcessDiagnoserNoneHandler);
    public override string ExpectedResult => "NoneResult";

    public override (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => default; // Returns default when RunMode is None

    public override IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase)
        => null; // Returns null when RunMode is None
}

public sealed class MockInProcessDiagnoserNoneHandler : BaseMockInProcessDiagnoserHandler
{
    public MockInProcessDiagnoserNoneHandler(string result) : base(result) { }
}