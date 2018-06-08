using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class JobConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constuctor without an array in the argument list
        protected JobConfigBaseAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        protected JobConfigBaseAttribute(params Job[] jobs)
        {
            Config = ManualConfig.CreateEmpty().With(jobs);
        }

        public IConfig Config { get; }
    }
}