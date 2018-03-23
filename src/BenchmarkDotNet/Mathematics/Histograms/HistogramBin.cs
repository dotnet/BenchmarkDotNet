using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Mathematics.Histograms
{
    public class HistogramBin
    {
        public double Lower { get; }
        public double Upper { get; }
        public double[] Values { get; }

        public int Count => Values.Length;
        public double Gap => Upper - Lower;
        public bool IsEmpty => Count == 0;
        public bool HasAny => Count > 0;

        public HistogramBin(double lower, double upper, double[] values)
        {
            Lower = lower;
            Upper = upper;
            Values = values;
        }

        public static HistogramBin Union(HistogramBin bin1, HistogramBin bin2) => new HistogramBin(
            Math.Min(bin1.Lower, bin2.Lower),
            Math.Max(bin1.Upper, bin2.Upper),
            bin1.Values.Union(bin2.Values).OrderBy(value => value).ToArray());

        public override string ToString()
        {
            var unit = TimeUnit.GetBestTimeUnit(Values);
            return $"[{Lower.ToTimeStr(unit)};{Upper.ToTimeStr(unit)}) {{{string.Join("; ", Values.Select(v => v.ToTimeStr(unit)))}}}";
        }
    }
}