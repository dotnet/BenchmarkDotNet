using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using System;
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

        [Fact]
        public void PowerPlanModeCharacteristicTest()
        {
            // null and Guid.Empty are both resolved as an empty string.
            Validate("", null);
            Validate("", Guid.Empty);

            // Other value is resolved to guid text representation.
            var guid = Guid.NewGuid();
            Validate(guid.ToString(), guid);

            static void Validate(string expected, Guid? guid)
            {
                var result = CharacteristicPresenter.DefaultPresenter.ToPresentation(guid, EnvironmentMode.PowerPlanModeCharacteristic);
                Assert.Equal(expected, result);
            }
        }
    }
}