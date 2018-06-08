using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class CategoriesColumnAttribute : ColumnConfigBaseAttribute
    {
        public CategoriesColumnAttribute() : base(CategoriesColumn.Default) { }
    }
}