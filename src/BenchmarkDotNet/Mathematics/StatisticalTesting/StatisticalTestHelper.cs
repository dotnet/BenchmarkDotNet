using System.Linq;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public static class StatisticalTestHelper
    {
        /// <summary>
        /// Two-one-sided t-tests
        /// </summary>
        public static TostResult<T> CalculateTost<T>(IOneSidedTest<T> test, double[] baseline, double[] candidate, Threshold threshold)
            where T : OneSidedTestResult
        {
            if (baseline.SequenceEqual(candidate)) // WelchTest would throw StackoverflowException ;)
                return new TostResult<T>(threshold, EquivalenceTestConclusion.Base, null, null);
            
            var fasterTestResult = test.IsGreater(baseline, candidate, threshold);
            var slowerTestResult = test.IsGreater(candidate, baseline, threshold);

            EquivalenceTestConclusion conclusion;
            if (fasterTestResult == null || slowerTestResult == null)
                conclusion = EquivalenceTestConclusion.Unknown;            
            else if (fasterTestResult.NullHypothesisIsRejected)
                conclusion = EquivalenceTestConclusion.Faster;
            else if (slowerTestResult.NullHypothesisIsRejected)
                conclusion = EquivalenceTestConclusion.Slower;
            else
                conclusion = EquivalenceTestConclusion.Same;

            return new TostResult<T>(threshold, conclusion, slowerTestResult, fasterTestResult);
        }
    }
}