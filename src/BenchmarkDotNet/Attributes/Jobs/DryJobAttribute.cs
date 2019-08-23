using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class DryJobAttribute : JobConfigBaseAttribute
    {
        public DryJobAttribute() : base(Job.Dry)
        {
        }

        /// <summary>
        /// defines a new Dry Job that targets specified Framework
        /// </summary>
        /// <param name="targetFrameworkMoniker">Target Framework to test.</param>
        public DryJobAttribute(TargetFrameworkMoniker targetFrameworkMoniker)
            : base(GetJob(targetFrameworkMoniker, null, null))
        {
        }

        /// <summary>
        /// defines a new Dry Job that targets specified Framework, JIT and Platform
        /// </summary>
        /// <param name="targetFrameworkMoniker">Target Framework to test.</param>
        public DryJobAttribute(TargetFrameworkMoniker targetFrameworkMoniker, Jit jit, Platform platform)
            : base(GetJob(targetFrameworkMoniker, jit, platform))
        {
        }

        private static Job GetJob(TargetFrameworkMoniker targetFrameworkMoniker, Jit? jit, Platform? platform)
        {
            var runtime = targetFrameworkMoniker.GetRuntime();
            var baseJob = Job.Dry.With(runtime).WithId($"Dry-{runtime.Name}");
            var id = baseJob.Id;

            if (jit.HasValue)
            {
                baseJob = baseJob.With(jit.Value);
                id += "-" + jit.Value;
            }

            if (platform.HasValue)
            {
                baseJob = baseJob.With(platform.Value);
                id += "-" + platform.Value;
            }

            return baseJob.WithId(id).Freeze();
        }
    }
}