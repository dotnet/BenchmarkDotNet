using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use [DryJob(RuntimeMoniker.Net$)] instead.", false)]
    public class DryClrJobAttribute : JobConfigBaseAttribute
    {
        public DryClrJobAttribute() : base(Job.Dry.WithRuntime(ClrRuntime.GetCurrentVersion()))
        {
        }
    }
}