using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)] // users must not be able to define given mutator attribute more than once per type
    public class JobMutatorConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor which use only CLS-compliant types
        public JobMutatorConfigBaseAttribute() => Config = ManualConfig.CreateEmpty();
        
        protected JobMutatorConfigBaseAttribute(Job job) => Config = ManualConfig.CreateEmpty().With(job.AsMutator());
        
        public IConfig Config { get; }
    }
}