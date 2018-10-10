using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class ModerateAccuracyJobAttribute : JobConfigBaseAttribute
    {
        public ModerateAccuracyJobAttribute() : base(Job.ModerateAccuracy)
        {
        }
    }
}
