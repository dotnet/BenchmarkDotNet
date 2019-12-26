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
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        public DryJobAttribute(RuntimeMoniker runtimeMoniker)
            : base(GetJob(runtimeMoniker, null, null))
        {
        }

        /// <summary>
        /// defines a new Dry Job that targets specified Framework, JIT and Platform
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        public DryJobAttribute(RuntimeMoniker runtimeMoniker, Jit jit, Platform platform)
            : base(GetJob(runtimeMoniker, jit, platform))
        {
        }

        private static Job GetJob(RuntimeMoniker runtimeMoniker, Jit? jit, Platform? platform)
        {
            var runtime = runtimeMoniker.GetRuntime();
            var baseJob = Job.Dry.WithRuntime(runtime).WithId($"Dry-{runtime.Name}");
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