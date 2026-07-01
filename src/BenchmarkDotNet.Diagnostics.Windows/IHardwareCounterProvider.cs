using BenchmarkDotNet.Diagnosers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows;

public interface IHardwareCounterProvider
{
    Dictionary<string, ProfileSourceInfo> GetAvailableCounters();

    void Configure(IEnumerable<PreciseMachineCounter> machineCounters);
}