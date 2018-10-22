using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class JobConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor which use only CLS-compliant types
        [PublicAPI]
        public JobConfigBaseAttribute() => Config = ManualConfig.CreateEmpty();

        protected JobConfigBaseAttribute(Job job) => Config = ManualConfig.CreateEmpty().With(job);

        public IConfig Config { get; }
    }
}