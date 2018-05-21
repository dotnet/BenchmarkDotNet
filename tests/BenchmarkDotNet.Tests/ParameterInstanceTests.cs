using BenchmarkDotNet.Parameters;
using System;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ParameterInstanceTests
    {
        private static readonly ParameterDefinition definition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>(), isArgument: false);

        [Theory]
        [InlineData(5)]
        [InlineData('e')]
        [InlineData("short")]
        public void ShortParameterValuesDisplayOriginalValue(object value)
        {
            var parameter = new ParameterInstance(definition, value);

            Assert.Equal(value.ToString(), parameter.ToDisplayText());
        }

        [Theory]
        [InlineData("text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7", "text/(...)q=0.7 [86]")]
        [InlineData("All the world's a stage, and all the men and women merely players: they have their exits and their entrances; and one man in his time plays many parts, his acts being seven ages.", "All t(...)ages. [178]")]
        [InlineData("this is a test to see what happens when we call tolower.", "this (...)ower. [56]")]
        public void VeryLongParameterValuesAreTrimmed(string initialLongText, string expectedDisplayText)
        {
            var parameter = new ParameterInstance(definition, initialLongText);

            Assert.NotEqual(initialLongText, parameter.ToDisplayText());

            Assert.Equal(expectedDisplayText, parameter.ToDisplayText());
        }
    }
}
