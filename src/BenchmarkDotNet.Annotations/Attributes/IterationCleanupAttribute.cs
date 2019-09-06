using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Marks method to be executed after each benchmark iteration. This should NOT be used for microbenchmarks - please see the docs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class IterationCleanupAttribute : TargetedAttribute
    {
    }
}