using System;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class VeryLongRunJobAttribute : JobConfigBaseAttribute
    {
        public VeryLongRunJobAttribute() : base(Job.VeryLongRun)
        {
        }

        /// <summary>
        /// defines a new VeryLongRun Job that targets specified Framework
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        public VeryLongRunJobAttribute(RuntimeMoniker runtimeMoniker)
            : base(GetJob(Job.VeryLongRun, runtimeMoniker, null, null))
        {
        }

        /// <summary>
        /// defines a new VeryLongRun Job that targets specified Framework, JIT and Platform
        /// </summary>
        /// <param name="runtimeMoniker">Target Framework to test.</param>
        /// <param name="jit">Jit to test.</param>
        /// <param name="platform">Platform to test.</param>
        public VeryLongRunJobAttribute(RuntimeMoniker runtimeMoniker, Jit jit, Platform platform)
            : base(GetJob(Job.VeryLongRun, runtimeMoniker, jit, platform))
        {
        }
    }
}