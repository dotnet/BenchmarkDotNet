using System;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    [PublicAPI]
    public class BenchmarkAttribute : Attribute
    {
        public string Description { get; set; }

        public bool Baseline { get; set; }

        public int OperationsPerInvoke { get; set; } = 1;

        public BenchmarkKind Kind { get; set; } = BenchmarkKind.MicroBenchmark;
    }
}