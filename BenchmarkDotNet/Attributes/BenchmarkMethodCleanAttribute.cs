using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BenchmarkMethodCleanAttribute : Attribute
    {
        public BenchmarkMethodCleanAttribute()
        {
        }

        public BenchmarkMethodCleanAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}