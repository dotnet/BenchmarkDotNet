using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class LogicalGroupColumnAttribute : ColumnConfigBaseAttribute
    {
        public LogicalGroupColumnAttribute() : base(LogicalGroupColumn.Default) { }
    }
}