using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class CategoriesColumnAttribute : ColumnConfigBaseAttribute
    {
        public CategoriesColumnAttribute() : base(CategoriesColumn.Default) { }
    }
}