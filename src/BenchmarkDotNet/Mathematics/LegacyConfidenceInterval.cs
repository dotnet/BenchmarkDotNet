using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;
using Perfolizer.Mathematics.Common;

namespace BenchmarkDotNet.Mathematics;

public enum LegacyConfidenceLevel
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
    private static readonly Dictionary<LegacyConfidenceLevel, (int value, int digits)> ConfidenceLevelDetails = CreateConfidenceLevelMapping();

    /// <summary>
    /// Calculates Z value (z-star) for confidence interval
    /// </summary>
    /// <param name="level">ConfidenceLevel for a confidence interval</param>
    /// <param name="n">Sample size (n >= 3)</param>
    public static double GetZValue(this LegacyConfidenceLevel level, int n)
    {
        return new ConfidenceIntervalEstimator(n, 0, 1).ZLevel(new ConfidenceLevel(level.ToPercent()));
    }

    public static string ToPercentStr(this LegacyConfidenceLevel level)
    {
        string s = level.ToString().Substring(1);
        if (s.Length > 2)
            s = s.Substring(0, 2) + "." + s.Substring(2);
        return s + "%";
    }

    [PublicAPI] public static double ToPercent(this LegacyConfidenceLevel level)
    {
        (int value, int digits) = ConfidenceLevelDetails[level];

        return value / Math.Pow(10, digits);
    }

    private static Dictionary<LegacyConfidenceLevel, (int value, int length)> CreateConfidenceLevelMapping()
        => Enum.GetValues(typeof(LegacyConfidenceLevel))
            .Cast<LegacyConfidenceLevel>()
            .ToDictionary(
                confidenceLevel => confidenceLevel,
                confidenceLevel =>
                {
                    string textRepresentation = confidenceLevel.ToString().Substring(1);

                    return (int.Parse(textRepresentation), textRepresentation.Length);
                });
}

public struct LegacyConfidenceInterval
{
    [PublicAPI] public int N { get; }
    [PublicAPI] public double Mean { get; }
    [PublicAPI] public double StandardError { get; }

    [PublicAPI] public LegacyConfidenceLevel Level { get; }
    [PublicAPI] public double Margin { get; }

    [PublicAPI] public double Lower { get; }
    [PublicAPI] public double Upper { get; }

    public LegacyConfidenceInterval(double mean, double standardError, int n, LegacyConfidenceLevel level = LegacyConfidenceLevel.L999)
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

    public override string ToString() => ToString(DefaultCultureInfo.Instance);

    public string ToString(CultureInfo cultureInfo, string format = "0.##", bool showLevel = true)
    {
        return ToString(x => x.ToString(format, cultureInfo), showLevel);
    }

    public string ToString(Func<double, string> formatter, bool showLevel = true)
    {
        var builder = new StringBuilder();
        builder.Append('[');
        builder.Append(formatter(Lower));
        builder.Append("; ");
        builder.Append(formatter(Upper));
        builder.Append("]");
        builder.Append(GetLevelHint(showLevel));
        return builder.ToString();
    }
}