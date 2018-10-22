namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class EquivalenceTestResult
    {
        public Threshold Threshold { get; }
        public EquivalenceTestConclusion Conclusion { get; }

        public EquivalenceTestResult(Threshold threshold, EquivalenceTestConclusion conclusion)
        {
            Threshold = threshold;
            Conclusion = conclusion;
        }

        public string H0 => Threshold.IsZero()
            ? "True difference in means is greater than zero"
            : $"True difference in means > {Threshold}";

        public string H1 => Threshold.IsZero()
            ? "True difference in means is zero"
            : $"True difference in means <= {Threshold}";
    }
}