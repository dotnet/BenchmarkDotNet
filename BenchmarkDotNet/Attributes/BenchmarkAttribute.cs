using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BenchmarkAttribute : Attribute
    {
        public BenchmarkAttribute()
        {
        }

        public BenchmarkAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}