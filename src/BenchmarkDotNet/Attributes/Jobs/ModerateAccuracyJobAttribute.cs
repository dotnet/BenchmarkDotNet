using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class ModerateAccuracyAttribute : JobConfigBaseAttribute
    {
        public ModerateAccuracyAttribute() : base(Job.ModerateAccuracy)
        {
        }
    }
}
