using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ParameterInstanceTests
    {
        private static readonly ParameterDefinition definition = new ParameterDefinition("Testing", isStatic: false, values: Array.Empty<object>(), isArgument: false, parameterType: null, 0);

        [Theory]
        [InlineData(5)]
        [InlineData('e')]
        [InlineData("short")]
        public void ShortParameterValuesDisplayOriginalValue(object value)
        {
            var parameter = new ParameterInstance(definition, value, null);

            Assert.Equal(value.ToString(), parameter.ToDisplayText());
        }

        [Theory]
        [InlineData("text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7", "text/(...)q=0.7 [86]")]
        [InlineData("All the world's a stage, and all the men and women merely players: they have their exits and their entrances; and one man in his time plays many parts, his acts being seven ages.", "All (...)ges. [178]")]
        [InlineData("this is a test to see what happens when we call tolower.", "this (...)ower. [56]")]
        public void VeryLongParameterValuesAreTrimmed(string initialLongText, string expectedDisplayText)
        {
            var parameter = new ParameterInstance(definition, initialLongText, null);

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
            var parameter = new ParameterInstance(definition, initialLongText, null);

            Assert.Equal(expectedDisplayText, parameter.ToDisplayText());
        }

        [Theory]
        [InlineData(typeof(ATypeWithAVeryVeryVeryVeryVeryVeryLongNameeeeeeeee), nameof(ATypeWithAVeryVeryVeryVeryVeryVeryLongNameeeeeeeee))]
        [InlineData(typeof(Guid), nameof(Guid))]
        [InlineData(typeof(Guid?), "Guid?")]
        [InlineData(typeof(List<int>), "List<Int32>")]
        public void TypeParameterValuesDisplayNotTrimmedTypeNameWithoutNamespace(Type type, string expectedName)
        {
            var parameter = new ParameterInstance(definition, type, null);

            Assert.Equal(expectedName, parameter.ToDisplayText());
        }

        [Theory]
        [InlineData("012345678901234567890", 21, "012345678901234567890")] // the default is 20
        [InlineData("0123456789012345678901234567890123456789", 30, "0123456789(...)0123456789 [40]")]
        [InlineData("😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀", 20, "😀😀(...)😀😀 [54]")]
        [InlineData("a😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀", 20, "a😀😀(...)😀😀 [55]")]
        [InlineData("😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀b", 20, "😀😀(...)😀😀b [55]")]
        [InlineData("a😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀b", 20, "a😀😀(...)😀😀b [56]")]
        public void MaxParameterColumnWidthCanBeCustomized(string initialLongText, int maxParameterColumnWidth, string expectedDisplayText)
        {
            var summaryStyle = SummaryStyle.Default.WithMaxParameterColumnWidth(maxParameterColumnWidth);
            var parameter = new ParameterInstance(definition, initialLongText, summaryStyle);

            Assert.Equal(expectedDisplayText, parameter.ToDisplayText());
        }

        [Theory]
        [InlineData(-100)]
        [InlineData(0)]
        [InlineData(10)]
        public void MaxParameterColumnWidthCanNotBeSetToValueLessThanDefault(int newWidth)
            => Assert.Throws<ArgumentOutOfRangeException>(() => SummaryStyle.Default.WithMaxParameterColumnWidth(newWidth));
    }

    public class ATypeWithAVeryVeryVeryVeryVeryVeryLongNameeeeeeeee { }
}
