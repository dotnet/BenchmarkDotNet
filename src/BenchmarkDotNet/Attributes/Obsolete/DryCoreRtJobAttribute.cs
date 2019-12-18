using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use [DryJob(RuntimeMoniker.CoreRt$)] instead.", false)]
    public class DryCoreRtJobAttribute : JobConfigBaseAttribute
    {
        public DryCoreRtJobAttribute() : base(Job.Dry.WithRuntime(CoreRtRuntime.GetCurrentVersion()))
        {
        }
    }
}