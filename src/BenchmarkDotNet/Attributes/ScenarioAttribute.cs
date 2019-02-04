using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    [PublicAPI]
    public class ScenarioAttribute : BenchmarkAttribute
    {
    }
}