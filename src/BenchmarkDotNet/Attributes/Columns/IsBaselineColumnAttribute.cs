using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class IsBaselineColumnAttribute : ColumnConfigBaseAttribute
    {
        public IsBaselineColumnAttribute() : base(IsBaselineColumn.Default) { }
    }
}