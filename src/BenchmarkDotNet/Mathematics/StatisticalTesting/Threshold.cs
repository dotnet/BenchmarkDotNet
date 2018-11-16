using System;
using System.Linq;
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

        public static bool TryParse(string input, out Threshold parsed)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                parsed = default;
                return false;
            }
            
            var trimmed = input.Trim().ToLowerInvariant();
            var number = new string(trimmed.TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            var unit = new string(trimmed.SkipWhile(c => char.IsDigit(c) || c == '.' || c == ',' || char.IsWhiteSpace(c)).ToArray());

            if (!double.TryParse(number, out var parsedValue) || !ThresholdUnitExtensions.ShortNameToUnit.TryGetValue(unit, out var parsedUnit))
            {
                parsed = default;
                return false;
            }

            parsed = parsedUnit == ThresholdUnit.Ratio ? Create(ThresholdUnit.Ratio, parsedValue / 100.0) : Create(parsedUnit, parsedValue);

            return true;
        }
    }
}