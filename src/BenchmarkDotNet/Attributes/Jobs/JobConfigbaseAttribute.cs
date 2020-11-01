using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
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

        protected JobConfigBaseAttribute(Job job) => Config = ManualConfig.CreateEmpty().AddJob(job);

        public IConfig Config { get; }

        protected static Job GetJob(Job sourceJob, RuntimeMoniker runtimeMoniker, Jit? jit, Platform? platform)
        {
            var runtime = runtimeMoniker.GetRuntime();
            var baseJob = sourceJob.WithRuntime(runtime).WithId($"{sourceJob.Id}-{runtime.Name}");
            var id = baseJob.Id;

            if (jit.HasValue)
            {
                baseJob = baseJob.WithJit(jit.Value);
                id += "-" + jit.Value;
            }

            if (platform.HasValue)
            {
                baseJob = baseJob.WithPlatform(platform.Value);
                id += "-" + platform.Value;
            }

            return baseJob.WithId(id).Freeze();
        }
    }
}