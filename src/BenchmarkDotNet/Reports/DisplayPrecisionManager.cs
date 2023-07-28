using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.Reports
{
    internal class DisplayPrecisionManager
    {
        private const int MinPrecision = 1;
        private const int MaxPrecision = 4;

        private readonly IDictionary<string, int> precision = new Dictionary<string, int>();
        private readonly Summary summary;

        public DisplayPrecisionManager(Summary summary) => this.summary = summary;

        /// <summary>
        /// Returns the best amount of decimal digits for the given column.
        /// </summary>
        public int GetPrecision(SummaryStyle summaryStyle, IStatisticColumn column, IStatisticColumn? parentColumn = null)
        {
            if (!precision.ContainsKey(column.Id))
            {
                var values = column.GetAllValues(summary, summaryStyle);
                precision[column.Id] = parentColumn != null
                    ? CalcPrecision(values, GetPrecision(summaryStyle, parentColumn))
                    : CalcPrecision(values);
            }

            return precision[column.Id];
        }

        internal static int CalcPrecision(IList<double> values)
        {
            if (values.IsEmpty())
                return MinPrecision;

            bool allValuesAreZeros = values.All(v => Math.Abs(v) < 1e-9);
            if (allValuesAreZeros)
                return MinPrecision;

            double minValue = values.Any() ? values.Min(v => Math.Abs(v)) : 0;
            if (double.IsNaN(minValue) || double.IsInfinity(minValue))
                return MinPrecision;
            if (minValue < 1 - 1e-9)
                return MaxPrecision;
            return MathHelper.Clamp((int) Math.Truncate(-Math.Log10(minValue)) + 3, MinPrecision, MaxPrecision);
        }

        internal static int CalcPrecision(IList<double> values, int parentPrecision)
        {
            return MathHelper.Clamp(CalcPrecision(values), parentPrecision, parentPrecision + 1);
        }
    }
}