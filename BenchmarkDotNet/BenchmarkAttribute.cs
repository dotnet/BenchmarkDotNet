using System;

namespace BenchmarkDotNet
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BenchmarkAttribute : Attribute
    {
        public BenchmarkAttribute()
        {
        }

        public BenchmarkAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; set; }

        public bool Baseline { get; set; }
    }
}