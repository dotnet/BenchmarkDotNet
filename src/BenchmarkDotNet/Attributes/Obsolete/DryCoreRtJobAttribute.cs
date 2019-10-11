using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use DryJobAttribute instead. Use the ctor that requires RuntimeMoniker argument.", false)]
    public class DryCoreRtJobAttribute : JobConfigBaseAttribute
    {
        public DryCoreRtJobAttribute() : base(Job.Dry.With(CoreRtRuntime.GetCurrentVersion()))
        {
        }
    }
}