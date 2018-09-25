using System;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Mathematics
{
    public abstract class Hypothesis
    {
        public abstract double GetThreshold(Statistics x);

        protected abstract bool IsZero();
        protected abstract string ToStr();

        public string H0
        {
            get
            {
                if (IsZero())
                    return "True difference in means is zero";
                return $"True difference in means <= {ToStr()}";
            }
        }

        public string H1
        {
            get
            {
                if (IsZero())
                    return "True difference in means is greater than zero";
                return $"True difference in means > {ToStr()}";
            }
        }
    }

    public class AbsoluteHypothesis : Hypothesis
    {
        private readonly TimeInterval threshold;

        public AbsoluteHypothesis(TimeInterval threshold)
        {
            this.threshold = threshold;
        }

        public override double GetThreshold(Statistics x) => threshold.Nanoseconds;

        protected override bool IsZero() => Math.Abs(threshold.Nanoseconds) < 1e-9;
        protected override string ToStr() => threshold.ToStr(format: "0.##");
    }

    public class RelativeHypothesis : Hypothesis
    {
        public static readonly Hypothesis Zero = new RelativeHypothesis(0);
        public static readonly Hypothesis Default = new RelativeHypothesis(0.01);

        private readonly double ratio;

        public RelativeHypothesis(double ratio) => this.ratio = ratio;

        public override double GetThreshold(Statistics x) => x.Mean * ratio;

        protected override bool IsZero() => Math.Abs(ratio) < 1e-9;
        protected override string ToStr() => ratio * 100 + "%";
    }
}