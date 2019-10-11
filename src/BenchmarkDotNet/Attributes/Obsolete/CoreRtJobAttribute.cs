using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use SimpleJobAttribute instead.", false)]
    public class CoreRtJobAttribute : JobConfigBaseAttribute
    {
        public CoreRtJobAttribute() : base(Job.Default.With(CoreRtRuntime.GetCurrentVersion()))
        {
        }
    }
}