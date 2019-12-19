using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class LegacyJitX86JobAttribute : JobConfigBaseAttribute
    {
        public LegacyJitX86JobAttribute() : base(Job.LegacyJitX86)
        {
        }
    }
}