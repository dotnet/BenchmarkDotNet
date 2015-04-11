using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BenchmarkCleanAttribute : Attribute
    {
        public BenchmarkCleanAttribute()
        {
        }

        public BenchmarkCleanAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}