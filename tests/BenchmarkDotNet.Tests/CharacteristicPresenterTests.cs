using System;
using BenchmarkDotNet.Extensions;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class CharacteristicPresenterTests
    {
        [Theory]
        [InlineData(1, 8, "00000001")]
        [InlineData(8, 8, "00001000")]
        [InlineData(0, 4, "0000")]
        public void ProcessorAffinityIsPrintedAsBitMask(int mask, int processorCount, string expectedResult)
        {
            Assert.Equal(expectedResult, ((IntPtr)mask).ToPresentation(processorCount));
        }
    }
}