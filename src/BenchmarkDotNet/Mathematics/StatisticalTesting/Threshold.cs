using System;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public abstract class Threshold
    {
        public static Threshold Create(ThresholdUnit unit, double value)
        {
            switch (unit)
            {
                case ThresholdUnit.Ratio: return new RelativeThreshold(value);
                case ThresholdUnit.Nanoseconds: return new AbsoluteTimeThreshold(TimeInterval.FromNanoseconds(value));
                case ThresholdUnit.Microseconds: return new AbsoluteTimeThreshold(TimeInterval.FromMicroseconds(value));
                case ThresholdUnit.Milliseconds: return new AbsoluteTimeThreshold(TimeInterval.FromMilliseconds(value));
                case ThresholdUnit.Seconds: return new AbsoluteTimeThreshold(TimeInterval.FromSeconds(value));
                case ThresholdUnit.Minutes: return new AbsoluteTimeThreshold(TimeInterval.FromMinutes(value));
                default: throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        public abstract double GetValue(Statistics x);
        public abstract bool IsZero();
    }
}