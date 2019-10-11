using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use SimpleJobAttribute instead.", false)]
    public class ClrJobAttribute : JobConfigBaseAttribute
    {
        public ClrJobAttribute() : base(Job.Default.With(ClrRuntime.GetCurrentVersion()))
        {
        }
    }
}