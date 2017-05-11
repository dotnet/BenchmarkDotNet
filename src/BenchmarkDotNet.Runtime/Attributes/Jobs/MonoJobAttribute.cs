using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class MonoJobAttribute : JobConfigBaseAttribute
    {
        public MonoJobAttribute() : base(Job.Mono)
        {
        }

        public MonoJobAttribute(string name, string path) 
            : base(new Job(name, new EnvMode(new MonoRuntime(name, path)).Freeze()).Freeze())
        {
        }
    }
}