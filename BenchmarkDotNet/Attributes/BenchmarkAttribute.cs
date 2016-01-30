using System;

namespace BenchmarkDotNet.Attributes
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

        public int OperationsPerInvoke { get; set; } = 1;
    }
}