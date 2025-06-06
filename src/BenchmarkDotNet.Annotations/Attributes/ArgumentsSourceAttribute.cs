using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ArgumentsSourceAttribute : PriorityAttribute
    {
        public string Name { get; }
        public Type? Type { get; }

        public ArgumentsSourceAttribute(string name)
        {
            Name = name;
            Type = null;
        }

        public ArgumentsSourceAttribute(Type type, string name)
        {
            Name = name;
            Type = type;
        }
    }
}