using System;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class RyuJitX86JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX86JobAttribute() : base(Job.RyuJitX86)
        {
        }
    }
}