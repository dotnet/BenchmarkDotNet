using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;
using Perfolizer;
using Perfolizer.Mathematics.QuantileEstimators;

namespace BenchmarkDotNet.Mathematics
{
    public class PercentileValues
    {
        [PublicAPI] public double Percentile(int percentile) => SimpleQuantileEstimator.Instance.Quantile(new Sample(SortedValues), percentile / 100.0);

        private IReadOnlyList<double> SortedValues { get; }

        public double P0 { get; }
        public double P25 { get; }
        public double P50 { get; }
        public double P67 { get; }
        public double P80 { get; }
        public double P85 { get; }
        public double P90 { get; }
        public double P95 { get; }
        public double P100 { get; }

        internal PercentileValues(IReadOnlyList<double> sortedValues)
        {
            SortedValues = sortedValues;

            // TODO: Collect all in one call?
            P0 = Percentile(0);
            P25 = Percentile(25);
            P50 = Percentile(50);
            P67 = Percentile(67);
            P80 = Percentile(80);
            P85 = Percentile(85);
            P90 = Percentile(90);
            P95 = Percentile(95);
            P100 = Percentile(100);
        }

        public override string ToString() => ToString(DefaultCultureInfo.Instance);

        public string ToString(Func<double, string> formatter)
        {
            var builder = new StringBuilder();
            builder.Append("[P95: ");
            builder.Append(formatter(P95));
            builder.Append("]; [P0: ");
            builder.Append(formatter(P0));
            builder.Append("]; [P50: ");
            builder.Append(formatter(P50));
            builder.Append("]; [P100: ");
            builder.Append(formatter(P100));
            builder.Append("]");
            return builder.ToString();
        }

        public string ToString(CultureInfo? cultureInfo, string format = "0.##")
        {
            return ToString(x => x.ToString(format, cultureInfo));
        }
    }
}