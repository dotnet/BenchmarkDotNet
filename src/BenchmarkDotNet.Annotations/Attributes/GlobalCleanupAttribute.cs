using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Marks method to be executed after all benchmark iterations.
    /// <remarks>It's going to be executed only once, after all benchmark runs.</remarks>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class GlobalCleanupAttribute : TargetedAttribute
    {
    }
}