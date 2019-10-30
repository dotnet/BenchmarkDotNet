using System;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class AbsoluteTimeThreshold : AbsoluteThreshold, IEquatable<AbsoluteTimeThreshold>
    {
        private readonly TimeValue timeValue;

        public AbsoluteTimeThreshold(TimeValue timeValue) : base(timeValue.Nanoseconds) => this.timeValue = timeValue;

        public override string ToString() => timeValue.ToString(DefaultCultureInfo.Instance, format: "0.##");

        public bool Equals(AbsoluteTimeThreshold other) => other != null && timeValue.Equals(other.timeValue);

        public override bool Equals(object obj) => obj is AbsoluteTimeThreshold other && Equals(other);

        public override int GetHashCode() => timeValue.GetHashCode();
    }
}