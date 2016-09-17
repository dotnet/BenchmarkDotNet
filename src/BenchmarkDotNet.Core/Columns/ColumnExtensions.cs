namespace BenchmarkDotNet.Columns
{
    public static class ColumnExtensions
    {
        public static IColumnProvider ToProvider(this IColumn column) => new SimpleColumnProvider(column);
    }
}