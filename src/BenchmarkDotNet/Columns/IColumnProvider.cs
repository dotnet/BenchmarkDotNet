using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Columns
{
    public interface IColumnProvider
    {
        IEnumerable<IColumn> GetColumns(Summary summary);
    }
}
