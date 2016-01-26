using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstances
    {
        public IList<ParameterInstance> Items { get; }

        public ParameterInstances(IList<ParameterInstance> items)
        {
            Items = items;
        }

        public string FullInfo => string.Join("_", Items.Select(p => $"{p.Name}-{p.Value}"));
    }
}