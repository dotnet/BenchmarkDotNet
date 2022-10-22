using System;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class LogicalGroupColumnAttribute : ColumnConfigBaseAttribute
    {
        public LogicalGroupColumnAttribute() : base(LogicalGroupColumn.Default) { }
    }
}