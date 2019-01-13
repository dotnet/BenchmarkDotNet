using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Engine.StoppingCriteria
{
    public class FixedStoppingCriteriaTests : StoppingCriteriaTestsBase
    {
        public FixedStoppingCriteriaTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        [InlineData(100)]
        public void EvaluateTest(int expectedCount)
        {
            var values = Enumerable.Range(0, expectedCount * 2).Select(x => (double) x).ToArray();
            var criteria = new FixedStoppingCriteria(expectedCount);
            ResolutionsAreCorrect(criteria, values, expectedCount);
        }

        [Theory]
        [InlineData(0, "FixedStoppingCriteria(iterationCount=0)")]
        [InlineData(1, "FixedStoppingCriteria(iterationCount=1)")]
        [InlineData(42, "FixedStoppingCriteria(iterationCount=42)")]
        public void TitleTest(int iterationTitle, string expectedTitle)
        {
            var criteria = new FixedStoppingCriteria(iterationTitle);
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
                "0", new WaringTestData(
                    new FixedStoppingCriteria(0),
                    Warnings.Empty)
            },
            {
                "1", new WaringTestData(
                    new FixedStoppingCriteria(1),
                    Warnings.Empty)
            },
            {
                "-1", new WaringTestData(
                    new FixedStoppingCriteria(-1),
                    new Warnings("Iteration count (-1) is negative"))
            }
        };

        [UsedImplicitly]
        public static TheoryData<string> WarningsDataNames = TheoryDataHelper.Create(WarningsData.Keys);
    }
}