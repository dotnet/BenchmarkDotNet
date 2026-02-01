using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstances : IEquatable<ParameterInstances>, IDisposable
    {
        public IReadOnlyList<ParameterInstance> Items { get; }
        public int Count => Items.Count;
        public ParameterInstance this[int index] => Items[index];
        public object? this[string name] => Items.FirstOrDefault(item => item.Name == name)?.Value;

        private string? printInfo = null;

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

        public bool Equals(ParameterInstances? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other == null)
                return false;

            if (other.Count != Count)
                return false;

            if (Count != other.Count)
                return false;

            for (int i = 0; i < Count; i++)
            {
                var currentItem = Items[i];
                var otherItem = other.Items[i];

                if (ReferenceEquals(currentItem, otherItem))
                    continue;

                if (currentItem is null || otherItem is null)
                    return false;

                if (!currentItem.Equals(otherItem.Value))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj) 
            => obj is ParameterInstances other && Equals(other);

        public override int GetHashCode() => FolderInfo.GetHashCode();
    }
}