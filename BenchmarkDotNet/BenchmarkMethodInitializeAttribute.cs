using System;

namespace BenchmarkDotNet
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BenchmarkMethodInitializeAttribute : Attribute
    {
        public BenchmarkMethodInitializeAttribute()
        {
        }

        public BenchmarkMethodInitializeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}