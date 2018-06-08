using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Add a column with the target method namespace.
    /// </summary>
    public class NamespaceColumnAttribute : ColumnConfigBaseAttribute
    {
        public NamespaceColumnAttribute() : base(TargetMethodColumn.Namespace)
        {
        }
    }
}