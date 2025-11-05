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
using System.Linq;

namespace BenchmarkDotNet.IntegrationTests.Diagnosers;

public abstract class BaseMockInProcessDiagnoser : IInProcessDiagnoser
{
    public Dictionary<BenchmarkCase, string> Results { get; } = [];
    public Dictionary<BenchmarkCase, List<BenchmarkSignal>> HandlerSignals { get; } = [];
    public Dictionary<BenchmarkCase, DateTime> FirstSignalTimes { get; } = [];

    public abstract string DiagnoserName { get; }
    public abstract RunMode DiagnoserRunMode { get; }
    public abstract string ExpectedResult { get; }

    public IEnumerable<string> Ids => [DiagnoserName];

    public IEnumerable<IExporter> Exporters => [];

    public IEnumerable<IAnalyser> Analysers => [];

    public void DisplayResults(ILogger logger) => logger.WriteLine($"{DiagnoserName} results: [{string.Join(", ", Results.Values)}]");

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) => DiagnoserRunMode;

    public void Handle(HostSignal signal, DiagnoserActionParameters parameters) { }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => [];

    public abstract (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase);

    public virtual IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase)
    {
        var (handlerType, serializedConfig) = GetSeparateProcessHandlerTypeAndSerializedConfig(benchmarkCase);
        if (handlerType == null)
            return null;
        var handler = (IInProcessDiagnoserHandler)Activator.CreateInstance(handlerType);
        handler.Initialize(serializedConfig);
        return handler;
    }

    public void DeserializeResults(BenchmarkCase benchmarkCase, string results)
    {
        // Parse the serialized results: "result|signals|timestamp"
        var parts = results.Split('|');
        var actualResult = parts[0];
        Results.Add(benchmarkCase, actualResult);

        if (parts.Length >= 3)
        {
            // Parse signals
            var signalsString = parts[1];
            if (!string.IsNullOrEmpty(signalsString))
            {
                var signals = signalsString.Split(',')
                    .Select(s => Enum.Parse<BenchmarkSignal>(s))
                    .ToList();
                HandlerSignals[benchmarkCase] = signals;
            }

            // Parse timestamp
            if (long.TryParse(parts[2], out var ticks))
            {
                FirstSignalTimes[benchmarkCase] = new DateTime(ticks, DateTimeKind.Utc);
            }
        }
    }
}

public abstract class BaseMockInProcessDiagnoserHandler : IInProcessDiagnoserHandler
{
    private string _result;
    private readonly List<BenchmarkSignal> _signals = [];
    private DateTime _firstSignalTime;

    protected BaseMockInProcessDiagnoserHandler() { }

    public void Initialize(string? serializedConfig)
    {
        _result = serializedConfig ?? string.Empty;
    }

    public void Handle(BenchmarkSignal signal, InProcessDiagnoserActionArgs args)
    {
        if (_signals.Count == 0)
        {
            _firstSignalTime = DateTime.UtcNow;
        }
        _signals.Add(signal);
    }

    public string SerializeResults()
    {
        // Encode the result with timing and signal information
        var signalsString = string.Join(",", _signals);
        var timestamp = _firstSignalTime.Ticks;
        return $"{_result}|{signalsString}|{timestamp}";
    }
}

public sealed class MockInProcessDiagnoser : BaseMockInProcessDiagnoser
{
    public override string DiagnoserName => nameof(MockInProcessDiagnoser);
    public override RunMode DiagnoserRunMode => RunMode.NoOverhead;
    public override string ExpectedResult => "MockResult";

    public override (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => (typeof(MockInProcessDiagnoserHandler), ExpectedResult);
}

public sealed class MockInProcessDiagnoserHandler : BaseMockInProcessDiagnoserHandler
{
}

public sealed class MockInProcessDiagnoserExtraRun : BaseMockInProcessDiagnoser
{
    public override string DiagnoserName => nameof(MockInProcessDiagnoserExtraRun);
    public override RunMode DiagnoserRunMode => RunMode.ExtraRun;
    public override string ExpectedResult => "ExtraRunResult";

    public override (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => (typeof(MockInProcessDiagnoserExtraRunHandler), ExpectedResult);
}

public sealed class MockInProcessDiagnoserExtraRunHandler : BaseMockInProcessDiagnoserHandler
{
}

public sealed class MockInProcessDiagnoserNone : BaseMockInProcessDiagnoser
{
    public override string DiagnoserName => nameof(MockInProcessDiagnoserNone);
    public override RunMode DiagnoserRunMode => RunMode.None;
    public override string ExpectedResult => "NoneResult";

    public override (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => default; // Returns default when RunMode is None

    public override IInProcessDiagnoserHandler? GetSameProcessHandler(BenchmarkCase benchmarkCase)
        => null; // Returns null when RunMode is None
}

public sealed class MockInProcessDiagnoserNoneHandler : BaseMockInProcessDiagnoserHandler
{
}

public sealed class MockInProcessDiagnoserSeparateLogic : BaseMockInProcessDiagnoser
{
    public override string DiagnoserName => nameof(MockInProcessDiagnoserSeparateLogic);
    public override RunMode DiagnoserRunMode => RunMode.SeparateLogic;
    public override string ExpectedResult => "SeparateLogicResult";

    public override (Type? handlerType, string? serializedConfig) GetSeparateProcessHandlerTypeAndSerializedConfig(BenchmarkCase benchmarkCase)
        => (typeof(MockInProcessDiagnoserSeparateLogicHandler), ExpectedResult);
}

public sealed class MockInProcessDiagnoserSeparateLogicHandler : BaseMockInProcessDiagnoserHandler
{
}