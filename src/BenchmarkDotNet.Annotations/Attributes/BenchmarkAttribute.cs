using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class BenchmarkAttribute : Attribute
    {
        public BenchmarkAttribute([CallerLineNumber] int sourceCodeLineNumber = 0) => SourceCodeLineNumber = sourceCodeLineNumber;

        public string Description { get; set; }

        public bool Baseline { get; set; }

        public int OperationsPerInvoke { get; set; } = 1;

        public int SourceCodeLineNumber { get; }
    }
}