using System.Collections.Generic;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Engine.StoppingCriteria
{
    public class AutoWarmupStoppingCriteriaTests : StoppingCriteriaTestsBase
    {
        public AutoWarmupStoppingCriteriaTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [MemberData(nameof(EvaluateData))]
        public void EvaluateTest(int minIterationCount, int maxIterationCount, int minFluctuationCount, double[] values, int expectedCount)
        {
            var criteria = new AutoWarmupStoppingCriteria(minIterationCount, maxIterationCount, minFluctuationCount);
            ResolutionsAreCorrect(criteria, values, expectedCount);
        }

        public static IEnumerable<object[]> EvaluateData()
        {
            yield return new object[] { 0, 8, 4, new double[] { 0, 10, 0, 10, 0, 10, 0, 10, 0, 10 }, 5 };
            yield return new object[] { 0, 5, 1, new double[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, 5 };
            yield return new object[] { 0, 5, 1, new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 2 };
            yield return new object[] { 0, 600, 2, new double[] { 10, 9, 8, 7, 8, 9, 8, 7, 8, 9 }, 7 };
            yield return new object[] { 5, 8, 0, new double[] { 0, 10, 0, 10, 0, 10, 0, 10, 0, 10 }, 5 };
        }

        [Theory]
        [InlineData(0, 0, 0, "AutoWarmupStoppingCriteria(minIterationCount=0, maxIterationCount=0, minFluctuationCount=0)")]
        [InlineData(1, 22, 333, "AutoWarmupStoppingCriteria(minIterationCount=1, maxIterationCount=22, minFluctuationCount=333)")]
        public void AutoWarmupTitleTest(int minIterationCount, int maxIterationCount, int minFluctuationCount, string expectedTitle)
        {
            var criteria = new AutoWarmupStoppingCriteria(minIterationCount, maxIterationCount, minFluctuationCount);
            Assert.Equal(expectedTitle, criteria.Title);
        }

        [Theory]
        [MemberData(nameof(WarningsDataNames))]
        public void WarningsTest(string dataName)
        {
            var (criteria, expectedWarnings) = WarningsData[dataName];
            Output.WriteLine("Criteria:" + criteria.Title);
            Assert.Equal(expectedWarnings.Messages, criteria.Warnings);
        }

        private static readonly IDictionary<string, WaringTestData> WarningsData = new Dictionary<string, WaringTestData>
        {
            {
                "0/0/0", new WaringTestData(
                    new AutoWarmupStoppingCriteria(0, 0, 0),
                    Warnings.Empty)
            },
            {
                "5/4/0", new WaringTestData(
                    new AutoWarmupStoppingCriteria(5, 4, 0),
                    new Warnings("Min Iteration Count (0) is greater than Max Iteration Count (4)"))
            },
            {
                "-5/0/0", new WaringTestData(
                    new AutoWarmupStoppingCriteria(-5, 0, 0),
                    new Warnings("Min Iteration Count (-5) is negative"))
            },
            {
                "0/0/-5", new WaringTestData(
                    new AutoWarmupStoppingCriteria(0, 0, -5),
                    new Warnings("Min Fluctuation Count (-5) is negative"))
            },
            {
                "0/-5/0", new WaringTestData(
                    new AutoWarmupStoppingCriteria(0, -5, 0),
                    new Warnings("Max Iteration Count (-5) is negative", "Min Iteration Count (0) is greater than Max Iteration Count (-5)"))
            }
        };

        [UsedImplicitly]
        public static TheoryData<string> WarningsDataNames = TheoryDataHelper.Create(WarningsData.Keys);
    }
}