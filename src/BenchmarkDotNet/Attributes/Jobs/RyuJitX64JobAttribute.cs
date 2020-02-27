using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class RyuJitX64JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX64JobAttribute() : base(Job.RyuJitX64)
        {
        }
    }
}