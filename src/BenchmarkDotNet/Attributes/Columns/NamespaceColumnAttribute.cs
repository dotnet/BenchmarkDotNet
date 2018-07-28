using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Add a column with the target method namespace.
    /// </summary>
    [PublicAPI]
    public class NamespaceColumnAttribute : ColumnConfigBaseAttribute
    {
        public NamespaceColumnAttribute() : base(TargetMethodColumn.Namespace)
        {
        }
    }
}