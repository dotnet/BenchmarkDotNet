using System;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Mathematics.StatisticalTesting;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class StatisticalTestColumn : BaselineCustomColumn
    {
        public static StatisticalTestColumn Create(StatisticalTestKind kind, Threshold threshold, bool showPValues = false)
            => new StatisticalTestColumn(kind, threshold, showPValues);

        public StatisticalTestKind Kind { get; }
        public Threshold Threshold { get; }
        public bool ShowPValues { get; }

        public StatisticalTestColumn(StatisticalTestKind kind, Threshold threshold, bool showPValues = false)
        {
            Kind = kind;
            Threshold = threshold;
            ShowPValues = showPValues;
        }

        public override string Id => nameof(StatisticalTestColumn) + "." + Kind + "." + Threshold + "." + (ShowPValues ? "WithDetails" : "WithoutDetails");
        public override string ColumnName => $"{Kind}({Threshold.ToString().Replace(" ", "")}){(ShowPValues ? "/p-values" : "")}";

        protected override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, Statistics current, bool isBaseline)
        {
            var x = baseline.GetOriginalValues().ToArray();
            var y = current.GetOriginalValues().ToArray();
            switch (Kind)
            {
                case StatisticalTestKind.Welch:
                    return StatisticalTestHelper.CalculateTost(WelchTest.Instance, x, y, Threshold).ToStr(ShowPValues);
                case StatisticalTestKind.MannWhitney:
                    return StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, x, y, Threshold).ToStr(ShowPValues);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override int PriorityInCategory => (int) Kind;
        public override bool IsNumeric => false;
        public override UnitType UnitType => UnitType.Dimensionless;

        public override string Legend => $"{Kind}-based TOST equivalence test with {Threshold} threshold{(ShowPValues ? ". Format: 'Result: p-value(Slower)|p-value(Faster)'" : "")}";
    }
}