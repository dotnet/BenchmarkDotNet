using System.Text;
using BenchmarkDotNet.Statistic;

namespace BenchmarkDotNet.Extensions
{
    public static class StatSummaryExtensions
    {
        private static string NullSummaryMessage = "<Empty statistic (N=0)>";

        public static string ToStr(this StatSummary s)
        {
            if (s == null)
                return NullSummaryMessage;
            var builder = new StringBuilder();
            builder.AppendLine($"Mean = {s.Mean.ToStr()}, StdError = {s.StandardError.ToStr()} (N = {s.N}, StdDev = {s.StandardDeviation.ToStr()})");
            builder.AppendLine($"Min = {s.Min.ToStr()}, Q1 = {s.Q1.ToStr()}, Median = {s.Median.ToStr()}, Q3 = {s.Q3.ToStr()}, Max = {s.Max.ToStr()}");
            builder.AppendLine($"IQR = {s.InterquartileRange.ToStr()}, LowerFence = {s.LowerFence.ToStr()}, UpperFence = {s.UpperFence.ToStr()}");
            builder.AppendLine($"ConfidenceInterval = {s.ConfidenceInterval.ToStr()}");
            return builder.ToString();
        }

        public static string ToTimeStr(this StatSummary s, TimeUnit unit = null)
        {
            if (s == null)
                return NullSummaryMessage;
            if (unit == null)
                unit = TimeUnit.GetBestTimeUnit(s.Mean);
            var builder = new StringBuilder();
            builder.AppendLine($"Mean = {s.Mean.ToTimeStr(unit)}, StdError = {s.StandardError.ToTimeStr(unit)} (N = {s.N}, StdDev = {s.StandardDeviation.ToTimeStr(unit)})");
            builder.AppendLine($"Min = {s.Min.ToTimeStr(unit)}, Q1 = {s.Q1.ToTimeStr(unit)}, Median = {s.Median.ToTimeStr(unit)}, Q3 = {s.Q3.ToTimeStr(unit)}, Max = {s.Max.ToTimeStr(unit)}");
            builder.AppendLine($"IQR = {s.InterquartileRange.ToTimeStr(unit)}, LowerFence = {s.LowerFence.ToTimeStr(unit)}, UpperFence = {s.UpperFence.ToTimeStr(unit)}");
            builder.AppendLine($"ConfidenceInterval = {s.ConfidenceInterval.ToTimeStr(unit)}");
            return builder.ToString();
        }
    }
}