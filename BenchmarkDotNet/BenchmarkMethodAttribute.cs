using System;

namespace BenchmarkDotNet
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BenchmarkMethodAttribute : Attribute
    {
        public BenchmarkMethodAttribute()
        {
        }

        public BenchmarkMethodAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}