using System;

namespace BenchmarkDotNet
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BenchmarkMethodAttribute: Attribute
    {
    }
}