using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class LongRunJobAttribute : JobConfigBaseAttribute
    {
        public LongRunJobAttribute() : base(Job.LongRun)
        {
        }

        /// <summary>
        /// defines a new LongRun Job that targets specified Framework
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        public LongRunJobAttribute(RuntimeMoniker runtimeMoniker)
            : base(GetJob(Job.LongRun, runtimeMoniker, null, null))
        {
        }

        /// <summary>
        /// defines a new LongRun Job that targets specified Framework, JIT and Platform
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        /// <param name="jit">Jit to test.</param>
        /// <param name="platform">Platform to test.</param>
        public LongRunJobAttribute(RuntimeMoniker runtimeMoniker, Jit jit, Platform platform)
            : base(GetJob(Job.LongRun, runtimeMoniker, jit, platform))
        {
        }
    }
}