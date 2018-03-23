using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Columns
{
    public class EmptyColumnProvider : IColumnProvider
    {
        public static readonly IColumnProvider Instance = new EmptyColumnProvider();

        private EmptyColumnProvider()
        {
        }

        public IEnumerable<IColumn> GetColumns(Summary summary) => Enumerable.Empty<IColumn>();
    }
}