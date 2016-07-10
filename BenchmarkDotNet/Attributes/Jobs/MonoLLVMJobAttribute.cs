using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class MonoLLVMJobAttribute : JobConfigBaseAttribute
    {
        public MonoLLVMJobAttribute() : base(Job.MonoLLVM)
        {
        }
    }
}