using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics.StatisticalTesting;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class StatisticalTestColumnAttribute : ColumnConfigBaseAttribute
    {
        public StatisticalTestColumnAttribute(StatisticalTestKind testKind, ThresholdUnit thresholdUnit, double value, bool showPValues = false)
            : base(StatisticalTestColumn.Create(testKind, Threshold.Create(thresholdUnit, value), showPValues)) { }

        public StatisticalTestColumnAttribute(StatisticalTestKind testKind, bool showPValues = false) : this(testKind, ThresholdUnit.Ratio, 0.1, showPValues) { }
        
        public StatisticalTestColumnAttribute(bool showPValues = false) : this(StatisticalTestKind.MannWhitney, showPValues) {}
    }

    [Obsolete("Use StatisticalTestAttribute")]
    public class WelchTTestPValueColumnAttribute : StatisticalTestColumnAttribute
    {
        public WelchTTestPValueColumnAttribute() : base(StatisticalTestKind.Welch) { }
    }
}