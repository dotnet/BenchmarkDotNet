using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class HardwareCountersAttribute : JobConfigBaseAttribute
    {
        public HardwareCountersAttribute()
        {
        }

        public HardwareCountersAttribute(params HardwareCounter[] counters) : base(new Job().WithHardwareCounters(counters))
        {
        }
    }
}