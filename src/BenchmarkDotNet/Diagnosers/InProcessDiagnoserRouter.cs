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

    internal static IInProcessDiagnoserHandler? CreateOrNull(InProcessDiagnoserHandlerData data)
        => data.HandlerType is null
        ? null
        : Init((IInProcessDiagnoserHandler) Activator.CreateInstance(data.HandlerType), data.SerializedConfig);
}