using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use DryJobAttribute instead. Use the ctor that requires RuntimeMoniker argument.", false)]
    public class DryMonoJobAttribute : JobConfigBaseAttribute
    {
        public DryMonoJobAttribute() : base(Job.Dry.With(MonoRuntime.Default))
        {
        }
    }
}