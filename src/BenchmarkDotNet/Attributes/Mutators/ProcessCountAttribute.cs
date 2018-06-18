using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// How many times we should launch process with target benchmark.
    /// </summary>
    public class ProcessCountAttribute : JobMutatorConfigBaseAttribute
    {
        public ProcessCountAttribute(int processLaunchCount) : base(Job.Default.WithLaunchCount(processLaunchCount))
        {
        }
    }
}