using System;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class RelativeThreshold : Threshold
    {
        public static readonly Threshold Zero = new RelativeThreshold(0);
        public static readonly Threshold Default = new RelativeThreshold(0.01);

        private readonly double ratio;

        public RelativeThreshold(double ratio) => this.ratio = ratio;

        public override double GetValue(Statistics x) => x.Mean * ratio;

        public override bool IsZero() => Math.Abs(ratio) < 1e-9;
        public override string ToString() => ratio * 100 + "%";
    }
}