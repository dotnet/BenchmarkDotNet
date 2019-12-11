using System;
using System.Globalization;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;
using JetBrains.Annotations;

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

        public override string ToString() => ToString(DefaultCultureInfo.Instance);

        [PublicAPI] public string ToString(CultureInfo cultureInfo)
        {
            var unit = TimeUnit.GetBestTimeUnit(Values);
            var builder = new StringBuilder();
            builder.Append('[');
            builder.Append(TimeInterval.FromNanoseconds(Lower).ToString(unit, cultureInfo));
            builder.Append(';');
            builder.Append(TimeInterval.FromNanoseconds(Upper).ToString(unit, cultureInfo));
            builder.Append(' ');
            builder.Append('{');
            for (var i = 0; i < Values.Length; i++)
            {
                if (i != 0)
                    builder.Append("; ");
                builder.Append(TimeInterval.FromNanoseconds(Values[i]).ToString(unit, cultureInfo));
            }
            builder.Append('}');
            
            return builder.ToString();
        }
    }
}