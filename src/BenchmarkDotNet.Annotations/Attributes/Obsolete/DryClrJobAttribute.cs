using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Plase use DryJobAttribute instead. Use the ctor that requires TargetFrameworkMoniker, Jit and Platform arguments.", true)]
    public class DryClrJobAttribute : Attribute
    {
    }
}