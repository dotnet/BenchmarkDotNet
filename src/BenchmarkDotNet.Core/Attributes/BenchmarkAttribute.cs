using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class BenchmarkAttribute : Attribute
    {
        public string Description { get; set; }

        public bool Baseline { get; set; }

        public int OperationsPerInvoke { get; set; } = 1;
    }
}