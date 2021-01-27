using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use [SimpleJob(RuntimeMoniker.Net$)] instead.", false)]
    public class ClrJobAttribute : JobConfigBaseAttribute
    {
        public ClrJobAttribute() : base(Job.Default.WithRuntime(ClrRuntime.GetCurrentVersion()))
        {
        }
    }
}