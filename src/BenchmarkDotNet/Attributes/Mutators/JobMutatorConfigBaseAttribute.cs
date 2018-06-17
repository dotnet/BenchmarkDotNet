using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class JobMutatorConfigBaseAttribute : JobConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constuctor which use only CLS-compliant types
        public JobMutatorConfigBaseAttribute() : base() { }
        
        protected JobMutatorConfigBaseAttribute(Job job) : base(job.AsMutator())
        {
        }
    }
}