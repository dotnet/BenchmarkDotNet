using System;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using Perfolizer.Phd;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Phd;

[PublicAPI]
public class BdnLifecycle : PhdLifecycle, IEquatable<BdnLifecycle>, IComparable<BdnLifecycle>
{
    public int LaunchIndex { get; set; }
    public IterationStage IterationStage { get; set; } = IterationStage.Unknown;
    public IterationMode IterationMode { get; set; } = IterationMode.Unknown;

    public bool Equals(BdnLifecycle? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return LaunchIndex == other.LaunchIndex &&
               IterationMode == other.IterationMode &&
               IterationStage == other.IterationStage;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((BdnLifecycle)obj);
    }

    public override int GetHashCode() => HashCode.Combine(LaunchIndex, IterationMode, IterationStage);

    public static bool operator ==(BdnLifecycle? left, BdnLifecycle? right) => Equals(left, right);
    public static bool operator !=(BdnLifecycle? left, BdnLifecycle? right) => !Equals(left, right);

    public int CompareTo(BdnLifecycle? other)
    {
        if (ReferenceEquals(this, other))
            return 0;
        if (ReferenceEquals(null, other))
            return 1;
        int launchIndexComparison = LaunchIndex.CompareTo(other.LaunchIndex);
        if (launchIndexComparison != 0)
            return launchIndexComparison;
        int iterationStageComparison = IterationStage.CompareTo(other.IterationStage);
        if (iterationStageComparison != 0)
            return iterationStageComparison;
        return IterationMode.CompareTo(other.IterationMode);
    }
}