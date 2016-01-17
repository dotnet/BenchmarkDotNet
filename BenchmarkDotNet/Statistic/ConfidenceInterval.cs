using System.Collections.Generic;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Statistic
{
    public enum ConfidenceLevel
    {
        L70, L75, L80, L85, L90, L92, L95, L96, L98, L99
    }

    public static class ConfidenceLevelExtensions
    {
        public static int ToPercent(this ConfidenceLevel level) => int.Parse(level.ToString().Substring(1));
    }

    public class ConfidenceInterval
    {
        private static readonly Dictionary<ConfidenceLevel, double> ZValues = new Dictionary<ConfidenceLevel, double>
        {
            [ConfidenceLevel.L70] = 1.04,
            [ConfidenceLevel.L75] = 1.15,
            [ConfidenceLevel.L80] = 1.28,
            [ConfidenceLevel.L85] = 1.44,
            [ConfidenceLevel.L90] = 1.645,
            [ConfidenceLevel.L92] = 1.75,
            [ConfidenceLevel.L95] = 1.96,
            [ConfidenceLevel.L96] = 2.05,
            [ConfidenceLevel.L98] = 2.33,
            [ConfidenceLevel.L99] = 2.58
        };

        public double Mean { get; }
        public double Error { get; }

        public ConfidenceLevel Level { get; }
        public double Margin { get; }

        public double Lower { get; }
        public double Upper { get; }

        public ConfidenceInterval(double mean, double error, ConfidenceLevel level = ConfidenceLevel.L95)
        {
            Mean = mean;
            Error = error;
            Level = level;
            Margin = error * ZValues[level];
            Lower = mean - Margin;
            Upper = mean + Margin;
        }

        public string ToStr() => $"[{Lower.ToStr()}; {Upper.ToStr()}] (CI {Level.ToPercent()}%)";
        public string ToTimeStr(TimeUnit unit) => $"[{Lower.ToTimeStr(unit)}; {Upper.ToTimeStr(unit)}] (CI {Level.ToPercent()}%)";
    }
}