using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Run the benchmark exactly once per iteration.
    /// </summary>
    public class RunOncePerIterationAttribute : JobMutatorConfigBaseAttribute
    {
        public RunOncePerIterationAttribute() : base(Job.Default.RunOncePerIteration())
        {
        }
    }
}