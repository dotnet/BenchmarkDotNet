using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// defines a new Job that targets specified Framework
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class TargetFrameworkJobAttribute : JobConfigBaseAttribute
    {
        /// <summary>
        /// defines a new Job that targets specified Framework
        /// </summary>
        /// <param name="targetFrameworkMoniker">Target Framework to test.</param>
        /// <param name="baseline">set to true if you want given Job to be a baseline for multiple runtimes comparison. False by default</param>
        public TargetFrameworkJobAttribute(TargetFrameworkMoniker targetFrameworkMoniker, bool baseline = false)
            : base(Job.Default
                .With(targetFrameworkMoniker.GetToolchain())
                .With(targetFrameworkMoniker.GetRuntime())
                .WithBaseline(baseline))
        {
        }
    }
}
