using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BenchmarkDotNet.IntegrationTests.Diagnosers;

public abstract class BaseMockInProcessDiagnoser(RunMode runMode) : IInProcessDiagnoser
{
    public static Queue<(RunMode runMode, int order, string result)> s_completedResults = new();

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
            : new(typeof(MockInProcessDiagnoserHandler), $"{GetSignal()} {ExpectedResult}");

    private BenchmarkSignal GetSignal() => RunMode switch
    {
        RunMode.NoOverhead => BenchmarkSignal.AfterActualRun,
        RunMode.ExtraIteration => BenchmarkSignal.AfterExtraIteration,
        RunMode.ExtraRun => BenchmarkSignal.AfterActualRun,
        _ => BenchmarkSignal.SeparateLogic
    };

    public void DeserializeResults(BenchmarkCase benchmarkCase, string results)
    {
        var split = results.Split(' ');
        int order = int.Parse(split[0]);
        string result = split[1];
        Results.Add(benchmarkCase, result);
        s_completedResults.Enqueue((RunMode, order, result));
    }
}

public sealed class MockInProcessDiagnoserHandler : IInProcessDiagnoserHandler
{
    private static int s_order;

    private BenchmarkSignal _signal;
    private string _result;

    public void Initialize(string? serializedConfig)
    {
        var split = serializedConfig!.Split(' ');
        _signal = (BenchmarkSignal) Enum.Parse(typeof(BenchmarkSignal), split[0]);
        _result = split[1];
    }

    public void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args)
    {
        if (signal == _signal)
        {
            _result = $"{Interlocked.Increment(ref s_order)} {_result}";
        }
    }

    public string SerializeResults() => _result;
}

// Diagnosers are made unique per-type rather than per-instance, so we have to create separate types to test multiple.
public sealed class MockInProcessDiagnoser1(RunMode runMode) : BaseMockInProcessDiagnoser(runMode) { }
public sealed class MockInProcessDiagnoser2(RunMode runMode) : BaseMockInProcessDiagnoser(runMode) { }
public sealed class MockInProcessDiagnoser3(RunMode runMode) : BaseMockInProcessDiagnoser(runMode) { }