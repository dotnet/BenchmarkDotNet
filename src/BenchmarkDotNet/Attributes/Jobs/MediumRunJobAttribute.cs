using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class MediumRunJobAttribute : JobConfigBaseAttribute
    {
        public MediumRunJobAttribute() : base(Job.MediumRun)
        {
        }

        /// <summary>
        /// defines a new MediumRun Job that targets specified Framework
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        public MediumRunJobAttribute(RuntimeMoniker runtimeMoniker)
            : base(GetJob(Job.MediumRun, runtimeMoniker, null, null))
        {
        }

        /// <summary>
        /// defines a new MediumRun Job that targets specified Framework, JIT and Platform
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        /// <param name="jit">Jit to test.</param>
        /// <param name="platform">Platform to test.</param>
        public MediumRunJobAttribute(RuntimeMoniker runtimeMoniker, Jit jit, Platform platform)
            : base(GetJob(Job.MediumRun, runtimeMoniker, jit, platform))
        {
        }
    }
}