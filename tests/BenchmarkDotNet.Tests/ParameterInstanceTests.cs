using BenchmarkDotNet.Parameters;
using System;
using System.Collections.Generic;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ParameterInstanceTests
    {
        private static readonly ParameterDefinition definition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>(), isArgument: false, parameterType: null);

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

        [Theory]
        [InlineData("0123456789012345", "0123456789012345")]
        [InlineData("01234567890123456", "01234567890123456")]
        [InlineData("012345678901234567", "012345678901234567")]
        [InlineData("0123456789012345678", "0123456789012345678")]
        [InlineData("01234567890123456789", "01234567890123456789")]
        [InlineData("012345678901234567890", "01234(...)67890 [21]")]
        public void TrimmingTheValuesMakesThemActuallyShorter(string initialLongText, string expectedDisplayText)
        {
            var parameter = new ParameterInstance(definition, initialLongText);
            
            Assert.Equal(expectedDisplayText, parameter.ToDisplayText());
        }

        [Theory]
        [InlineData(typeof(ATypeWithAVeryVeryVeryVeryVeryVeryLongNameeeeeeeee), nameof(ATypeWithAVeryVeryVeryVeryVeryVeryLongNameeeeeeeee))]
        [InlineData(typeof(Guid), nameof(Guid))]
        [InlineData(typeof(Guid?), "Guid?")]
        [InlineData(typeof(List<int>), "List<Int32>")]
        public void TypeParameterValuesDisplayNotTrimmedTypeNameWithoutNamespace(Type type, string expectedName)
        {
            var parameter = new ParameterInstance(definition, type);

            Assert.Equal(expectedName, parameter.ToDisplayText());
        }
    }
    
    public class ATypeWithAVeryVeryVeryVeryVeryVeryLongNameeeeeeeee { }
}
