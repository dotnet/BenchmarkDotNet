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

        public string ValueInfo => Items.Any() ? "[" + string.Join(", ", Items.Select(p => $"{p.Name}={GetValueInfo(p.Value)}")) + "]" : "";

        public string PrintInfo => printInfo ?? (printInfo = string.Join("&", Items.Select(p => $"{p.Name}={p.ToDisplayText()}")));

        private static string GetValueInfo(object value)
        {
            if (value is not Array array)
                return value?.ToString() ?? "null";

            var strings = new List<string>(array.Length); //test array of array
            for (int i = 0; i < array.Length; i++)
            {
                string str = GetValueInfo(array.GetValue(i));
                strings.Add(str);
            }

            return string.Join(",", strings);
        }

        public ParameterInstance GetArgument(string name) => Items.Single(parameter => parameter.IsArgument && parameter.Name == name);

        public bool Equals(ParameterInstances other)
        {
            if (other.Count != Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (!Items[i].Value.Equals(other[i].Value))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => obj is ParameterInstances other && Equals(other);

        public override int GetHashCode() => FolderInfo.GetHashCode();
    }
}