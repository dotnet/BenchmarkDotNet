using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use SimpleJobAttribute instead.", false)]
    public class CoreJobAttribute : JobConfigBaseAttribute
    {
        public CoreJobAttribute() : base(Job.Default.With(CoreRuntime.GetCurrentVersion()))
        {
        }
    }
}