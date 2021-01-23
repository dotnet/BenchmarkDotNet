using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstances : IDisposable
    {
        public IReadOnlyList<ParameterInstance> Items { get; }
        public int Count => Items.Count;
        public ParameterInstance this[int index] => Items[index];
        public object this[string name] => Items.FirstOrDefault(item => item.Name == name)?.Value;

        private string printInfo;

        public ParameterInstances(IReadOnlyList<ParameterInstance> items)
        {
            Items = items;
        }

        public void Dispose()
        {
            foreach (var parameterInstance in Items)
            {
                parameterInstance.Dispose();
            }
        }

        public string FolderInfo => string.Join("_", Items.Select(p => $"{p.Name}-{p.ToDisplayText()}")).AsValidFileName();

        public string DisplayInfo =>  Items.Any() ? "[" + string.Join(", ", Items.Select(p => $"{p.Name}={p.ToDisplayText()}")) + "]" : "";

        public string ValueInfo => Items.Any() ? "[" + string.Join(", ", Items.Select(p => $"{p.Name}={p.Value?.ToString() ?? ParameterInstance.NullParameterTextRepresentation}")) + "]" : "";

        public string PrintInfo => printInfo ?? (printInfo = string.Join("&", Items.Select(p => $"{p.Name}={p.ToDisplayText()}")));

        public ParameterInstance GetArgument(string name) => Items.Single(parameter => parameter.IsArgument && parameter.Name == name);
    }
}