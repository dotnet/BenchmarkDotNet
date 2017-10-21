using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsSourceAttribute : Attribute
    {
        public string Name { get; }

        public ParamsSourceAttribute(string name) => Name = name;
    }
}