using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use [SimpleJob(RuntimeMoniker.NetCoreApp$)] instead.", false)]
    public class CoreJobAttribute : JobConfigBaseAttribute
    {
        public CoreJobAttribute() : base(Job.Default.WithRuntime(CoreRuntime.GetCurrentVersion()))
        {
        }
    }
}