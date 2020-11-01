using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class MonoJobAttribute : JobConfigBaseAttribute
    {
        public MonoJobAttribute(bool baseline = false) : base(Job.Default.WithRuntime(MonoRuntime.Default).WithBaseline(baseline))
        {
        }

        public MonoJobAttribute(string name, string path, bool baseline = false)
            : base(new Job(name, new EnvironmentMode(new MonoRuntime(name, path)).Freeze()).WithBaseline(baseline).Freeze())
        {
        }
    }
}