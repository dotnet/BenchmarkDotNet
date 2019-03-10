using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Marks method to be executed before all benchmark iterations.
    /// <remarks>It's going to be executed only once, just before warm up.</remarks>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class GlobalSetupAttribute : TargetedAttribute
    {
    }
}