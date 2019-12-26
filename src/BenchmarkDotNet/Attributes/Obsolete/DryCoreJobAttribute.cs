using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use [DryJob(RuntimeMoniker.NetCoreApp$)] instead.", false)]
    public class DryCoreJobAttribute : JobConfigBaseAttribute
    {
        public DryCoreJobAttribute() : base(Job.Dry.WithRuntime(CoreRuntime.GetCurrentVersion()))
        {
        }
    }
}