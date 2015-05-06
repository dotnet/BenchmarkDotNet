using System;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkMeasurementStatistic
    {
        public string Name { get; }
        public long Min { get; }
        public long Max { get; }
        public long Median { get; }
        public double StandardDeviation { get; }
        public double Error { get; }

        public BenchmarkMeasurementStatistic(string name, long[] values)
        {
            Name = name;
            if (values.Length == 0)
            {
                Min = Max = Median = 00;
                StandardDeviation = Error = double.PositiveInfinity;
            }
            else
            {
                Min = values.Min();
                Max = values.Max();
                Median = values.Median();
                StandardDeviation = values.StandardDeviation();
                Error = (Max - Min) * 1.0 / Min;
            }
        }
    }
}