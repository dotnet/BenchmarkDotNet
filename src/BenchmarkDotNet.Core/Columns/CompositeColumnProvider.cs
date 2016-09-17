using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Columns
{
    public class CompositeColumnProvider : IColumnProvider
    {
        private readonly IColumnProvider[] providers;

        public CompositeColumnProvider(params IColumnProvider[] providers)
        {
            this.providers = providers;
        }

        public IEnumerable<IColumn> GetColumns(Summary summary) => providers.SelectMany(p => p.GetColumns(summary));
    }
}