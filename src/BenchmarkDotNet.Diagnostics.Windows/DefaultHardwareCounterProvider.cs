using BenchmarkDotNet.Diagnosers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows;

public sealed class DefaultHardwareCounterProvider : IHardwareCounterProvider
{
    public static readonly DefaultHardwareCounterProvider Instance = new ();

    public Dictionary<string, ProfileSourceInfo> GetAvailableCounters() => TraceEventProfileSources.GetInfo();

    public void Configure(IEnumerable<PreciseMachineCounter> counters)
    {
        TraceEventProfileSources.Set( // it's a must have to get the events enabled!!
            counters.Select(counter => counter.ProfileSourceId).ToArray(),
            counters.Select(counter => counter.Interval).ToArray());
    }
}