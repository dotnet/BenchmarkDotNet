using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Plase use DryJobAttribute instead. Use the ctor that requires TargetFrameworkMoniker argument.", true)]
    public class DryMonoJobAttribute : Attribute
    {
    }
}