using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class JobConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor which use only CLS-compliant types
        public JobConfigBaseAttribute() => Config = ManualConfig.CreateEmpty();
        
        public JobConfigBaseAttribute(Job job) => Config = ManualConfig.CreateEmpty().With(job);

        public IConfig Config { get; }
    }
}