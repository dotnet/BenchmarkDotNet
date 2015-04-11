using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BenchmarkInitializeAttribute : Attribute
    {
        public BenchmarkInitializeAttribute()
        {
        }

        public BenchmarkInitializeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}