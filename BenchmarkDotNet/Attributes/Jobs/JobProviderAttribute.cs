using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class JobProviderAttribute : Attribute, IConfigSource
    {
        protected JobProviderAttribute(IJob job)
        {
            Config = ManualConfig.CreateEmpty().With(job);
        }

        public IConfig Config { get; }
    }
}