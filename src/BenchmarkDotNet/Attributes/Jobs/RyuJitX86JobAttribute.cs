using System;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class RyuJitX86JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX86JobAttribute() : base(Job.RyuJitX86)
        {
        }
    }
}