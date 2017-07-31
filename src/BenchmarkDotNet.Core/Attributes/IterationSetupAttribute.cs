using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Marks method to be executed before each benchmark iteration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class IterationSetupAttribute: TargetedAttribute
    {
    }
}
