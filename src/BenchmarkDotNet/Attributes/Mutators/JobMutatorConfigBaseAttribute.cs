using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)] // users must not be able to define given mutator attribute more than once per type
    public class JobMutatorConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor which use only CLS-compliant types
        [PublicAPI]
        public JobMutatorConfigBaseAttribute() => Config = ManualConfig.CreateEmpty();

        protected JobMutatorConfigBaseAttribute(Job job) => Config = ManualConfig.CreateEmpty().AddJob(job.AsMutator());

        public IConfig Config { get; }
    }
}