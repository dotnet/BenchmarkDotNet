using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class JobConfigBaseAttribute : Attribute, IConfigSource
    {
        protected JobConfigBaseAttribute(params IJob[] jobs)
        {
            Config = ManualConfig.CreateEmpty().With(jobs);
        }

        public IConfig Config { get; }
    }
}