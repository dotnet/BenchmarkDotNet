using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class LogicalGroupColumnAttribute : ColumnConfigBaseAttribute
    {
        public LogicalGroupColumnAttribute() : base(LogicalGroupColumn.Default) { }
    }
}