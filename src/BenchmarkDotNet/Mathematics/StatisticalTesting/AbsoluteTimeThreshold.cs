using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class AbsoluteTimeThreshold : AbsoluteThreshold
    {
        private readonly TimeInterval timeInterval;

        public AbsoluteTimeThreshold(TimeInterval timeInterval) : base(timeInterval.Nanoseconds)
        {
            this.timeInterval = timeInterval;
        }

        public override string ToString() => timeInterval.ToStr(format: "0.##");
    }
}