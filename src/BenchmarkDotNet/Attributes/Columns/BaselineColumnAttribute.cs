using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class BaselineColumnAttribute : ColumnConfigBaseAttribute
    {
        public BaselineColumnAttribute() : base(BaselineColumn.Default) { }
    }
}