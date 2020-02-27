using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class LegacyJitX64JobAttribute : JobConfigBaseAttribute
    {
        public LegacyJitX64JobAttribute() : base(Job.LegacyJitX64)
        {
        }
    }
}