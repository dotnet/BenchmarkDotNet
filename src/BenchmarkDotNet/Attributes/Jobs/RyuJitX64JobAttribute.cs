using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class RyuJitX64JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX64JobAttribute() : base(Job.RyuJitX64)
        {
        }
    }
}