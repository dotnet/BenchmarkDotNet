using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class BenchmarkAttribute : Attribute
    {
        public string Description { get; set; }

        public bool Baseline { get; set; }

        public int OperationsPerInvoke { get; set; } = 1;
    }
}