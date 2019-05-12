using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Marks method to be executed before every benchmark invocation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class InvocationSetupAttribute : TargetedAttribute
    {
    }
}
