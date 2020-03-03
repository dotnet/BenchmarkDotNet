using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [Obsolete("Please use [DryJob(RuntimeMoniker.Mono)] instead.", false)]
    public class DryMonoJobAttribute : JobConfigBaseAttribute
    {
        public DryMonoJobAttribute() : base(Job.Dry.WithRuntime(MonoRuntime.Default))
        {
        }
    }
}