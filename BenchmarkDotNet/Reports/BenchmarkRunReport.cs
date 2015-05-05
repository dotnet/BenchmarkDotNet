using System;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkRunReport
    {
        public long Value { get; }
        public string Unit { get; }

        public BenchmarkRunReport(long value, string unit)
        {
            Value = value;
            Unit = unit;
        }

        public static BenchmarkRunReport Parse(string line)
        {
            var split = line.
                Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1].
                Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return new BenchmarkRunReport(long.Parse(split[0]), split[1]);
        }
    }
}