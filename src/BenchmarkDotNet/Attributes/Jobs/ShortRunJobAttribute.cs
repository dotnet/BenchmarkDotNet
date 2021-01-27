using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class ShortRunJobAttribute: JobConfigBaseAttribute
    {
        public ShortRunJobAttribute() : base(Job.ShortRun)
        {
        }

        /// <summary>
        /// defines a new ShortRun Job that targets specified Framework
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        public ShortRunJobAttribute(RuntimeMoniker runtimeMoniker)
            : base(GetJob(Job.ShortRun, runtimeMoniker, null, null))
        {
        }

        /// <summary>
        /// defines a new ShortRun Job that targets specified Framework, JIT and Platform
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        /// <param name="jit">Jit to test.</param>
        /// <param name="platform">Platform to test.</param>
        public ShortRunJobAttribute(RuntimeMoniker runtimeMoniker, Jit jit, Platform platform)
            : base(GetJob(Job.ShortRun, runtimeMoniker, jit, platform))
        {
        }
    }
}