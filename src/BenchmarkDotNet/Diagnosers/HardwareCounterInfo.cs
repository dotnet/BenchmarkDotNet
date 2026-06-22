using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnosers;

[SuppressMessage("ReSharper", "IdentifierTypo")]
public readonly struct HardwareCounterInfo(string fullName, string shortName) : IEquatable<HardwareCounterInfo>
{
    public readonly string FullName = fullName;
    public readonly string ShortName = shortName;
    public readonly bool TheGreaterTheBetter = false;

    public static HardwareCounterInfo From(HardwareCounter counter)
    {
        return new HardwareCounterInfo(counter.ToString(), counter.ToShortName());
    }

    public static HardwareCounterInfo Parse(string counterName)
    {
        // Old
        if (Enum.TryParse(counterName, true, out HardwareCounter counter))
        {
            return new HardwareCounterInfo(counter.ToString(), counter.ToShortName());
        }

        // New
        var availableCpuCounters = TraceEventProfileSources.GetInfo();
        if (availableCpuCounters.TryGetValue(counterName, out var cpuCounter))
        {
            return new HardwareCounterInfo(cpuCounter.Name, counterName);
        }

        throw new ArgumentException($"Unknown hardware counter {counterName}");
    }

    public bool Equals(HardwareCounterInfo other) => FullName == other.FullName;

    public override bool Equals(object? obj) => obj is HardwareCounterInfo other && Equals(other);

    public override int GetHashCode() => FullName != null ? FullName.GetHashCode() : 0;

    public static bool operator ==(HardwareCounterInfo left, HardwareCounterInfo right) => left.Equals(right);

    public static bool operator !=(HardwareCounterInfo left, HardwareCounterInfo right) => !left.Equals(right);
}