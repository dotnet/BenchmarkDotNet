using System;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class AbsoluteTimeThreshold : AbsoluteThreshold, IEquatable<AbsoluteTimeThreshold>
    {
        private readonly TimeInterval timeInterval;

        public AbsoluteTimeThreshold(TimeInterval timeInterval) : base(timeInterval.Nanoseconds) => this.timeInterval = timeInterval;

        public override string ToString() => timeInterval.ToStr(format: "0.##");

        public bool Equals(AbsoluteTimeThreshold other) => other != null && timeInterval.Equals(other.timeInterval);

        public override bool Equals(object obj) => obj is AbsoluteTimeThreshold other && Equals(other);

        public override int GetHashCode() => timeInterval.GetHashCode();
    }
}