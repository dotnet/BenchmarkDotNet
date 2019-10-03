using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterDefinitions
    {
        [PublicAPI] public IReadOnlyList<ParameterDefinition> Items { get; }

        public ParameterDefinitions(IReadOnlyList<ParameterDefinition> items) => Items = items;

        public IReadOnlyList<ParameterInstances> Expand(SummaryStyle summaryStyle) => Expand(new[] { new ParameterInstances(new List<ParameterInstance>()) }, Items, summaryStyle);

        private static IReadOnlyList<ParameterInstances> Expand(IReadOnlyList<ParameterInstances> instancesList, IReadOnlyList<ParameterDefinition> definitions, SummaryStyle summaryStyle)
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
                    items.Add(new ParameterInstance(nextDefinition, value, summaryStyle));
                    newInstancesList.Add(new ParameterInstances(items));
                }
            }
            return Expand(newInstancesList, definitions.Skip(1).ToArray(), summaryStyle);
        }

        public override string ToString() => Items.Any() ? string.Join(",", Items.Select(item => item.Name)) : "<empty>";
    }
}