using System;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Mathematics;
using JetBrains.Annotations;
using Perfolizer.Horology;
using Perfolizer.Mathematics.Histograms;
using Perfolizer.Mathematics.Multimodality;

namespace BenchmarkDotNet.Extensions
{
    public static class StatisticsExtensions
    {
        private const string NullSummaryMessage = "<Empty statistic (N=0)>";

        public static Func<double, string> CreateNanosecondFormatter(this Statistics s, CultureInfo cultureInfo, string format = "N3")
        {
            var timeUnit = TimeUnit.GetBestTimeUnit(s.Mean);
            return x => TimeInterval.FromNanoseconds(x).ToString(timeUnit, cultureInfo, format);
        }

        [PublicAPI]
        public static string ToString(this Statistics s, CultureInfo cultureInfo, Func<double, string> formatter, bool calcHistogram = false)
        {
            if (s == null)
                return NullSummaryMessage;

            string listSeparator = cultureInfo.GetActualListSeparator();

            var builder = new StringBuilder();
            string errorPercent = (s.StandardError / s.Mean * 100).ToString("0.00", cultureInfo);
            var ci = s.ConfidenceInterval;
            string ciMarginPercent = (ci.Margin / s.Mean * 100).ToString("0.00", cultureInfo);
            double mValue = MValueCalculator.Calculate(s.OriginalValues);

            builder.Append("Mean = ");
            builder.Append(formatter(s.Mean));
            builder.Append(listSeparator);
            builder.Append(" StdErr = ");
            builder.Append(formatter(s.StandardError));
            builder.Append(" (");
            builder.Append(errorPercent);
            builder.Append("%)");
            builder.Append(listSeparator);
            builder.Append(" N = ");
            builder.Append(s.N.ToString(cultureInfo));
            builder.Append(listSeparator);
            builder.Append(" StdDev = ");
            builder.Append(formatter(s.StandardDeviation));
            builder.AppendLine();

            builder.Append("Min = ");
            builder.Append(formatter(s.Min));
            builder.Append(listSeparator);
            builder.Append(" Q1 = ");
            builder.Append(formatter(s.Q1));
            builder.Append(listSeparator);
            builder.Append(" Median = ");
            builder.Append(formatter(s.Median));
            builder.Append(listSeparator);
            builder.Append(" Q3 = ");
            builder.Append(formatter(s.Q3));
            builder.Append(listSeparator);
            builder.Append(" Max = ");
            builder.Append(formatter(s.Max));
            builder.AppendLine();

            builder.Append("IQR = ");
            builder.Append(formatter(s.InterquartileRange));
            builder.Append(listSeparator);
            builder.Append(" LowerFence = ");
            builder.Append(formatter(s.LowerFence));
            builder.Append(listSeparator);
            builder.Append(" UpperFence = ");
            builder.Append(formatter(s.UpperFence));
            builder.AppendLine();

            builder.Append("ConfidenceInterval = ");
            builder.Append(s.ConfidenceInterval.ToString(formatter));
            builder.Append(listSeparator);
            builder.Append(" Margin = ");
            builder.Append(formatter(ci.Margin));
            builder.Append(" (");
            builder.Append(ciMarginPercent);
            builder.Append("% of Mean)");
            builder.AppendLine();

            builder.Append("Skewness = ");
            builder.Append(s.Skewness.ToString("0.##", cultureInfo));
            builder.Append(listSeparator);
            builder.Append(" Kurtosis = ");
            builder.Append(s.Kurtosis.ToString("0.##", cultureInfo));
            builder.Append(listSeparator);
            builder.Append(" MValue = ");
            builder.Append(mValue.ToString("0.##", cultureInfo));
            builder.AppendLine();

            if (calcHistogram)
            {
                var histogram = HistogramBuilder.Adaptive.Build(s.OriginalValues);
                builder.AppendLine("-------------------- Histogram --------------------");
                builder.AppendLine(histogram.ToString(formatter));
                builder.AppendLine("---------------------------------------------------");
            }
            return builder.ToString().Trim();
        }
    }
}