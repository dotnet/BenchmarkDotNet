using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsSourceAttribute : PriorityAttribute
    {
        public string Name { get; }
        public Type? Type { get; }

        public ParamsSourceAttribute(string name)
        {
            Name = name;
            Type = null;
        }

        public ParamsSourceAttribute(Type type, string name)
        {
            Name = name;
            Type = type;
        }
    }
}