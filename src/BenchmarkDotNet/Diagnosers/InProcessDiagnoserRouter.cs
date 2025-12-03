using BenchmarkDotNet.Running;
using JetBrains.Annotations;
using System;
using System.ComponentModel;

namespace BenchmarkDotNet.Diagnosers;

[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct InProcessDiagnoserRouter
{
    public IInProcessDiagnoserHandler handler;
    public int index;
    public RunMode runMode;

    public static IInProcessDiagnoserHandler Init(IInProcessDiagnoserHandler handler, string serializedConfig)
    {
        handler.Initialize(serializedConfig);
        return handler;
    }

    internal static InProcessDiagnoserRouter Create(IInProcessDiagnoser diagnoser, BenchmarkCase benchmarkCase, int index)
    {
        var data = diagnoser.GetHandlerData(benchmarkCase);
        if (data.HandlerType is null)
        {
            return default;
        }
        return new()
        {
            handler = Init((IInProcessDiagnoserHandler) Activator.CreateInstance(data.HandlerType), data.SerializedConfig),
            index = index,
            runMode = diagnoser.GetRunMode(benchmarkCase)
        };
    }
}