using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class InProcessAttribute : JobConfigBaseAttribute
    {
        public InProcessAttribute(bool dontLogOutput = false) : base(dontLogOutput ? Job.InProcessDontLogOutput : Job.InProcess)
        {
        }
    }
}