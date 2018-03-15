using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ArgumentsSourceAttribute : Attribute
    {
        public string Name { get; }

        public ArgumentsSourceAttribute(string name) => Name = name;
    }
}