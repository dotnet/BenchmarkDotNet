using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Marks method to be executed after every benchmark invocation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class InvocationCleanupAttribute : TargetedAttribute
    {
    }
}
