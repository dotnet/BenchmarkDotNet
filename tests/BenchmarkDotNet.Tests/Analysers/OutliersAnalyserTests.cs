using BenchmarkDotNet.Analysers;
using Xunit;

namespace BenchmarkDotNet.Tests.Analysers
{
    public class OutliersAnalyserTests
    {
        [Theory]
        [InlineData(0, 0, "")]
        [InlineData(1, 1, "1 outlier  was  removed")]
        [InlineData(2, 2, "2 outliers were removed")]
        [InlineData(3, 3, "3 outliers were removed")]
        [InlineData(0, 1, "1 outlier  was  detected")]
        [InlineData(0, 2, "2 outliers were detected")]
        [InlineData(0, 3, "3 outliers were detected")]
        [InlineData(1, 2, "1 outlier  was  removed, 2 outliers were detected")]
        [InlineData(2, 3, "2 outliers were removed, 3 outliers were detected")]
        public void MessageTest(int actualOutliers, int allOutliers, string expectedMessage)
        {
            string actualMessage = OutliersAnalyser.GetMessage(actualOutliers, allOutliers);
            Assert.Equal(expectedMessage, actualMessage);
        }
    }
}