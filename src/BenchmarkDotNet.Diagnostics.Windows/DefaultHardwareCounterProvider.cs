using BenchmarkDotNet.Diagnosers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows;

public sealed class DefaultHardwareCounterProvider : IHardwareCounterProvider
{
    public static readonly DefaultHardwareCounterProvider Instance = new ();

    public Dictionary<string, ProfileSourceInfo> GetAvailableCounters() => TraceEventProfileSources.GetInfo();

    public void Configure(IEnumerable<PreciseMachineCounter> machineCounters)
    {
        TraceEventProfileSources.Set( // it's a must have to get the events enabled!!
            machineCounters.Select(counter => counter.ProfileSourceId).ToArray(),
            machineCounters.Select(counter => counter.Interval).ToArray());
    }
}