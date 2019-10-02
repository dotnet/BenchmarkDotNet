using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use TargetFrameworkAttribute instead.", true)]
    public class ClrJobAttribute : Attribute
    {
    }
}