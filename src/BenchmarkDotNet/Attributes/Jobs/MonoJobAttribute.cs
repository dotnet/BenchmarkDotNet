using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class MonoJobAttribute : JobConfigBaseAttribute
    {
        public MonoJobAttribute(bool isBaseline = false) : base(Job.Mono.WithIsBaseline(isBaseline))
        {
        }

        public MonoJobAttribute(string name, string path, bool isBaseline = false) 
            : base(new Job(name, new EnvMode(new MonoRuntime(name, path)).Freeze()).WithIsBaseline(isBaseline).Freeze())
        {
        }
    }
}