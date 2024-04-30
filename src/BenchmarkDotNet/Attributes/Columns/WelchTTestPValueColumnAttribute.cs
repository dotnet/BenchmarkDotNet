using System;
using BenchmarkDotNet.Columns;
using JetBrains.Annotations;
using Perfolizer.Mathematics.Common;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class StatisticalTestColumnAttribute : ColumnConfigBaseAttribute
    {
        public StatisticalTestColumnAttribute() : base(StatisticalTestColumn.Create("10%", null)) { }

        public StatisticalTestColumnAttribute(string threshold) : base(StatisticalTestColumn.Create(threshold, null)) { }

        public StatisticalTestColumnAttribute(string threshold, SignificanceLevel significanceLevel)
            : base(StatisticalTestColumn.Create(threshold, significanceLevel)) { }
    }
}