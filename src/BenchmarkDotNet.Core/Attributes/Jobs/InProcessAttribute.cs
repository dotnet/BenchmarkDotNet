using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class InProcessAttribute : JobConfigBaseAttribute
    {
        public InProcessAttribute(bool dontLogOutput = false) : base(dontLogOutput ? Job.InProcessDontLogOutput : Job.InProcess)
        {
        }
    }
}