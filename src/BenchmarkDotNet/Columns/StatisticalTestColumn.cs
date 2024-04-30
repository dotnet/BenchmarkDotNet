using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;

namespace BenchmarkDotNet.Columns
{
    public class StatisticalTestColumn(Threshold threshold, SignificanceLevel? significanceLevel = null) : BaselineCustomColumn
    {
        private static readonly SignificanceLevel DefaultSignificanceLevel = SignificanceLevel.P1E5;

        public static StatisticalTestColumn CreateDefault() => new (new PercentValue(10).ToThreshold());

        public static StatisticalTestColumn Create(Threshold threshold, SignificanceLevel? significanceLevel = null) => new (threshold, significanceLevel);

        public static StatisticalTestColumn Create(string threshold, SignificanceLevel? significanceLevel = null)
        {
            if (!Threshold.TryParse(threshold, out var parsedThreshold))
                throw new ArgumentException($"Can't parse threshold '{threshold}'");
            return new StatisticalTestColumn(parsedThreshold, significanceLevel);
        }

        public Threshold Threshold { get; } = threshold;
        public SignificanceLevel SignificanceLevel { get; } = significanceLevel ?? DefaultSignificanceLevel;

        public override string Id => $"{nameof(StatisticalTestColumn)}/{Threshold}";
        public override string ColumnName => $"MannWhitney({Threshold})";

        public override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, IReadOnlyDictionary<string, Metric> baselineMetrics,
            Statistics current, IReadOnlyDictionary<string, Metric> currentMetrics, bool isBaseline)
        {
            if (baseline.Sample.Values.SequenceEqual(current.Sample.Values))
                return "Baseline";
            if (current.Sample.Size == 1 && baseline.Sample.Size == 1)
                return "?";

            var test = new SimpleEquivalenceTest(MannWhitneyTest.Instance);
            var comparisonResult = test.Perform(current.Sample, baseline.Sample, Threshold, SignificanceLevel);
            return comparisonResult switch
            {
                ComparisonResult.Greater => "Slower",
                ComparisonResult.Indistinguishable => "Same",
                ComparisonResult.Lesser => "Faster",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override int PriorityInCategory => 0;
        public override bool IsNumeric => false;
        public override UnitType UnitType => UnitType.Dimensionless;

        public override string Legend => $"MannWhitney-based equivalence test (threshold={Threshold}, alpha = {SignificanceLevel})";
    }
}