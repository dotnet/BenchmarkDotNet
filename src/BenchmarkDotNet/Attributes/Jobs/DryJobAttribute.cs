using BenchmarkDotNet.Environments;
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
            : base(GetJob(Job.Dry, runtimeMoniker, null, null))
        {
        }

        /// <summary>
        /// defines a new Dry Job that targets specified Framework, JIT and Platform
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        /// <param name="jit">Jit to test.</param>
        /// <param name="platform">Platform to test.</param>
        public DryJobAttribute(RuntimeMoniker runtimeMoniker, Jit jit, Platform platform)
            : base(GetJob(Job.Dry, runtimeMoniker, jit, platform))
        {
        }
    }
}