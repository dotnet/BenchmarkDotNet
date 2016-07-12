using System.Collections.Generic;

namespace BenchmarkDotNet.Columns
{
    // This interface is an IDiagnoser that wants to provide it's results via extra columns
    // When the Summary Table is built, these extra columns are included in the table
    public interface IColumnProvider
    {
        IEnumerable<IColumn> GetColumns { get; }
    }
}
