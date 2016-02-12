using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterDefinitions
    {
        public IList<ParameterDefinition> Items { get; }

        public ParameterDefinitions(IList<ParameterDefinition> items)
        {
            Items = items;
        }

        public IList<ParameterInstances> Expand() => Expand(new[] { new ParameterInstances(new List<ParameterInstance>()) }, Items);

        private static IList<ParameterInstances> Expand(IList<ParameterInstances> instancesList, IList<ParameterDefinition> definitions)
        {
            if (definitions.IsNullOrEmpty())
                return instancesList;
            var nextDefinition = definitions.First();
            var newInstancesList = new List<ParameterInstances>();
            foreach (var instances in instancesList)
            {
                foreach (var value in nextDefinition.Values)
                {
                    var items = new List<ParameterInstance>();
                    items.AddRange(instances.Items);
                    items.Add(new ParameterInstance(nextDefinition, value));
                    newInstancesList.Add(new ParameterInstances(items));
                }
            }
            return Expand(newInstancesList, definitions.Skip(1).ToArray());
        }

        public override string ToString() => Items.Any() ? string.Join(",", Items.Select(item => item.Name)) : "<empty>";
    }
}