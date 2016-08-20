using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    /// <summary>
    /// Add a column with the target method namespace.
    /// </summary>
    public class NamespaceColumnAttribute : ColumnConfigBaseAttribute
    {
        public NamespaceColumnAttribute() : base(PropertyColumn.Namespace)
        {
        }
    }
}