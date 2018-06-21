using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
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