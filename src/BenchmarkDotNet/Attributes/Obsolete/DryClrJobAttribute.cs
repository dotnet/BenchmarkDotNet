using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use DryJobAttribute instead. Use the ctor that requires RuntimeMoniker, Jit and Platform arguments.", false)]
    public class DryClrJobAttribute : JobConfigBaseAttribute
    {
        public DryClrJobAttribute() : base(Job.Dry.With(ClrRuntime.GetCurrentVersion()))
        {
        }
    }
}