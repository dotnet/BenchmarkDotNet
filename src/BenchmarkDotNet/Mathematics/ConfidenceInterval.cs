using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics
{
    public enum ConfidenceLevel
    {
        /// <summary>
        /// 50.0% confidence interval
        /// </summary>
        [PublicAPI] L50,

        /// <summary>
        /// 70.0% confidence interval
        /// </summary>
        [PublicAPI] L70,

        /// <summary>
        /// 75.0% confidence interval
        /// </summary>
        [PublicAPI] L75,

        /// <summary>
        /// 80.0% confidence interval
        /// </summary>
        [PublicAPI] L80,

        /// <summary>
        /// 85.0% confidence interval
        /// </summary>
        [PublicAPI] L85,

        /// <summary>
        /// 90.0% confidence interval
        /// </summary>
        [PublicAPI] L90,

        /// <summary>
        /// 92.0% confidence interval
        /// </summary>
        [PublicAPI] L92,

        /// <summary>
        /// 95.0% confidence interval
        /// </summary>
        [PublicAPI] L95,

        /// <summary>
        /// 96.0% confidence interval
        /// </summary>
        [PublicAPI] L96,

        /// <summary>
        /// 97.0% confidence interval
        /// </summary>
        [PublicAPI] L97,

        /// <summary>
        /// 98.0% confidence interval
        /// </summary>
        [PublicAPI] L98,

        /// <summary>
        /// 99.0% confidence interval
        /// </summary>
        [PublicAPI] L99,

        /// <summary>
        /// 99.9% confidence interval
        /// </summary>
        [PublicAPI] L999
    }

    public static class ConfidenceLevelExtensions
    {
        private static readonly Dictionary<ConfidenceLevel, (int value, int digits)> ConfidenceLevelDetails = CreateConfidenceLevelMapping();

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

        [PublicAPI] public static double ToPercent(this ConfidenceLevel level)
        {
            (int value, int digits) = ConfidenceLevelDetails[level];

            return value / Math.Pow(10, digits);
        }

        private static Dictionary<ConfidenceLevel, (int value, int length)> CreateConfidenceLevelMapping()
            => Enum.GetValues(typeof(ConfidenceLevel))
                   .Cast<ConfidenceLevel>()
                   .ToDictionary(
                       confidenceLevel => confidenceLevel,
                       confidenceLevel =>
                       {
                           string textRepresentation = confidenceLevel.ToString().Substring(1);

                           return (int.Parse(textRepresentation), textRepresentation.Length);
                       });
    }

    public struct ConfidenceInterval
    {
        [PublicAPI] public int N { get; }
        [PublicAPI] public double Mean { get; }
        [PublicAPI] public double StandardError { get; }

        [PublicAPI] public ConfidenceLevel Level { get; }
        [PublicAPI] public double Margin { get; }

        [PublicAPI] public double Lower { get; }
        [PublicAPI] public double Upper { get; }

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

        private string GetLevelHint(bool showLevel = true) => showLevel ? $" (CI {Level.ToPercentStr()})" : "";

        public string ToStr(bool showLevel = true) => $"[{Lower.ToStr()}; {Upper.ToStr()}]{GetLevelHint(showLevel)}";

        public string ToTimeStr(Encoding encoding, TimeUnit unit = null, bool showLevel = true) =>
            $"[{Lower.ToTimeStr(unit, encoding)}; {Upper.ToTimeStr(unit, encoding)}]{GetLevelHint(showLevel)}";
    }
}