using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class QuickRoughAccuracyAttribute : JobConfigBaseAttribute
    {
        public QuickRoughAccuracyAttribute() : base(Job.QuickRough)
        {
        }
    }
}
