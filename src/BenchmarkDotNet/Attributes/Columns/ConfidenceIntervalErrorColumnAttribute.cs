﻿using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class ConfidenceIntervalErrorColumnAttribute : ColumnConfigBaseAttribute
    {
        public ConfidenceIntervalErrorColumnAttribute(ConfidenceLevel level = ConfidenceLevel.L999) : base(StatisticColumn.CiError(level))
        {
        }
    }
}