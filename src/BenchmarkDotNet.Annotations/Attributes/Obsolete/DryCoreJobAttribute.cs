using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use DryJobAttribute instead. Use the ctor that requires TargetFrameworkMoniker argument.", true)]
    public class DryCoreJobAttribute : Attribute
    {
    }
}