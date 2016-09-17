using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class RankColumnAttribute : ColumnConfigBaseAttribute
    {
        public RankColumnAttribute(RankColumn.Kind kind = RankColumn.Kind.Arabic) : base(new RankColumn(kind))
        {
        }
    }
}