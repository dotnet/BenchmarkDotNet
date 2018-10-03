using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    class CustomEnvironmentInfoAttribute : Attribute
    {
    }
}
