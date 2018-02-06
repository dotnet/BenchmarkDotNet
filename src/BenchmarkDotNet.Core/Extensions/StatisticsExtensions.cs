using System.Text;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Mathematics.Histograms;

namespace BenchmarkDotNet.Extensions
{
    public static class StatisticsExtensions
    {
        private static string NullSummaryMessage = "<Empty statistic (N=0)>";

        public static string ToStr(this Statistics s)
        {
            if (s == null)
                return NullSummaryMessage;
            var builder = new StringBuilder();
            string errorPercent = (s.StandardError / s.Mean * 100).ToStr("0.00");
            var ci = s.ConfidenceInterval;
            string ciMarginPercent = (ci.Margin / s.Mean * 100).ToStr("0.00");
            builder.AppendLine($"Mean = {s.Mean.ToStr()}, StdErr = {s.StandardError.ToStr()} ({errorPercent}%); N = {s.N}, StdDev = {s.StandardDeviation.ToStr()}");
            builder.AppendLine($"Min = {s.Min.ToStr()}, Q1 = {s.Q1.ToStr()}, Median = {s.Median.ToStr()}, Q3 = {s.Q3.ToStr()}, Max = {s.Max.ToStr()}");
            builder.AppendLine($"IQR = {s.InterquartileRange.ToStr()}, LowerFence = {s.LowerFence.ToStr()}, UpperFence = {s.UpperFence.ToStr()}");
            builder.AppendLine($"ConfidenceInterval = {ci.ToStr()}, Margin = {ci.Margin.ToStr()} ({ciMarginPercent}% of Mean)");
            builder.AppendLine($"Skewness = {s.Skewness.ToStr()}, Kurtosis = {s.Kurtosis.ToStr()}");
            return builder.ToString();
        }

        public static string ToTimeStr(this Statistics s, TimeUnit unit = null, bool calcHistogram = false)
        {
            if (s == null)
                return NullSummaryMessage;
            if (unit == null)
                unit = TimeUnit.GetBestTimeUnit(s.Mean);
            var builder = new StringBuilder();
            string errorPercent = (s.StandardError / s.Mean * 100).ToStr("0.00");
            var ci = s.ConfidenceInterval;
            string ciMarginPercent = (ci.Margin / s.Mean * 100).ToStr("0.00");
            double mValue = MathHelper.CalculateMValue(s);
            builder.AppendLine($"Mean = {s.Mean.ToTimeStr(unit)}, StdErr = {s.StandardError.ToTimeStr(unit)} ({errorPercent}%); N = {s.N}, StdDev = {s.StandardDeviation.ToTimeStr(unit)}");
            builder.AppendLine($"Min = {s.Min.ToTimeStr(unit)}, Q1 = {s.Q1.ToTimeStr(unit)}, Median = {s.Median.ToTimeStr(unit)}, Q3 = {s.Q3.ToTimeStr(unit)}, Max = {s.Max.ToTimeStr(unit)}");
            builder.AppendLine($"IQR = {s.InterquartileRange.ToTimeStr(unit)}, LowerFence = {s.LowerFence.ToTimeStr(unit)}, UpperFence = {s.UpperFence.ToTimeStr(unit)}");
            builder.AppendLine($"ConfidenceInterval = {s.ConfidenceInterval.ToTimeStr(unit)}, Margin = {ci.Margin.ToTimeStr(unit)} ({ciMarginPercent}% of Mean)");
            builder.AppendLine($"Skewness = {s.Skewness.ToStr()}, Kurtosis = {s.Kurtosis.ToStr()}, MValue = {mValue.ToStr()}");
            if (calcHistogram)
            {
                var histogram = HistogramBuilder.Adaptive.Build(s);
                builder.AppendLine("-------------------- Histogram --------------------");
                builder.AppendLine(histogram.ToTimeStr());
                builder.AppendLine("---------------------------------------------------");
            }
            return builder.ToString().Trim();
        }
    }
}