using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class ClrJobAttribute : JobConfigBaseAttribute
    {
        public ClrJobAttribute(bool isBaseline = false) : base(Job.Clr.WithIsBaseline(isBaseline))
        {
        }
    }
}