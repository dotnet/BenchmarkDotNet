using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class AllJitsJobAttribute : JobConfigBaseAttribute
    {
        public AllJitsJobAttribute() : base(Job.AllJits)
        {
        }
    }
}