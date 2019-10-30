using System;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class AbsoluteThreshold : Threshold
    {
        private readonly double value;

        public AbsoluteThreshold(double value)
        {
            this.value = value;
        }

        public override double GetValue(Statistics x) => value;

        public override bool IsZero() => Math.Abs(value) < 1e-9;
        public override string ToString() => value.ToString("0.##", DefaultCultureInfo.Instance);
    }
}