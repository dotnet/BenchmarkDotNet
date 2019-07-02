using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Plase use TargetFrameworkAttribute instead.", true)]
    public class ClrJobAttribute : Attribute
    {
    }
}