using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Mathematics
{
    public enum ConfidenceLevel
    {
        /// <summary>
        /// 50.0% confidence interval
        /// </summary>
        L50,

        /// <summary>
        /// 70.0% confidence interval
        /// </summary>
        L70,

        /// <summary>
        /// 75.0% confidence interval
        /// </summary>
        L75,

        /// <summary>
        /// 80.0% confidence interval
        /// </summary>
        L80,

        /// <summary>
        /// 85.0% confidence interval
        /// </summary>
        L85,

        /// <summary>
        /// 90.0% confidence interval
        /// </summary>
        L90,

        /// <summary>
        /// 92.0% confidence interval
        /// </summary>
        L92,

        /// <summary>
        /// 95.0% confidence interval
        /// </summary>
        L95,

        /// <summary>
        /// 96.0% confidence interval
        /// </summary>
        L96,

        /// <summary>
        /// 97.0% confidence interval
        /// </summary>
        L97,

        /// <summary>
        /// 98.0% confidence interval
        /// </summary>
        L98,

        /// <summary>
        /// 99.0% confidence interval
        /// </summary>
        L99,

        /// <summary>
        /// 99.9% confidence interval
        /// </summary>
        L999
    }

    public static class ConfidenceLevelExtensions
    {
        /// <summary>
        /// Calculates Z value (z-star) for confidence interval
        /// </summary>
        /// <param name="level">ConfidenceLevel for a confidence interval</param>
        /// <param name="n">Sample size (n >= 3)</param>
        public static double GetZValue(this ConfidenceLevel level, int n)
        {
            if (n <= 2)
                throw new ArgumentOutOfRangeException(nameof(n), "n should be >= 3");
            return MathHelper.InverseStudent(1 - level.ToPercent(), n - 1);
        }

        public static string ToPercentStr(this ConfidenceLevel level)
        {
            string s = level.ToString().Substring(1);
            if (s.Length > 2)
                s = s.Substring(0, 2) + "." + s.Substring(2);
            return s + "%";
        }

        public static double ToPercent(this ConfidenceLevel level)
        {
            (int value, int digits) = GetDetailsWithoutAllocation(level);

            return value / Math.Pow(10, digits);
        }

        private static (int value, int digits) GetDetailsWithoutAllocation(ConfidenceLevel level)
        {
            switch (level)
            {
                case ConfidenceLevel.L50:
                    return (50, 2);
                case ConfidenceLevel.L70:
                    return (70, 2);
                case ConfidenceLevel.L75:
                    return (75, 2);
                case ConfidenceLevel.L80:
                    return (80, 2);
                case ConfidenceLevel.L85:
                    return (85, 2);
                case ConfidenceLevel.L90:
                    return (90, 2);
                case ConfidenceLevel.L92:
                    return (92, 2);
                case ConfidenceLevel.L95:
                    return (95, 2);
                case ConfidenceLevel.L96:
                    return (96, 2);
                case ConfidenceLevel.L97:
                    return (97, 2);
                case ConfidenceLevel.L98:
                    return (98, 2);
                case ConfidenceLevel.L99:
                    return (99, 2);
                case ConfidenceLevel.L999:
                    return (999, 3);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }

    public struct ConfidenceInterval
    {
        public int N { get; }
        public double Mean { get; }
        public double StandardError { get; }

        public ConfidenceLevel Level { get; }
        public double Margin { get; }

        public double Lower { get; }
        public double Upper { get; }

        public ConfidenceInterval(double mean, double standardError, int n, ConfidenceLevel level = ConfidenceLevel.L999)
        {
            N = n;
            Mean = mean;
            StandardError = standardError;
            Level = level;
            Margin = n <= 2 ? double.NaN : standardError * level.GetZValue(n);
            Lower = mean - Margin;
            Upper = mean + Margin;
        }

        public bool Contains(double value) => Lower - 1e-9 < value && value < Upper + 1e-9;

        public string ToStr(bool showLevel = true) => $"[{Lower.ToStr()}; {Upper.ToStr()}] (CI {Level.ToPercentStr()})";

        public string ToTimeStr(TimeUnit unit = null, bool showLevel = true) =>
            $"[{Lower.ToTimeStr(unit)}; {Upper.ToTimeStr(unit)}] (CI {Level.ToPercentStr()})";
    }
}