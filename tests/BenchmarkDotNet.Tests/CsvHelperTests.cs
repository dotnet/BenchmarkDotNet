using BenchmarkDotNet.Exporters.Csv;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class CsvHelperTests
    {
        [Theory]
        [InlineData("Just a text", "Just a text")]
        [InlineData("Text with \n linebreak", "\"Text with \n linebreak\"")]
        [InlineData("Text with \r tab", "\"Text with \r tab\"")]
        [InlineData("Text with letters, comma", "\"Text with letters, comma\"")]
        [InlineData("Text; with separator", "\"Text; with separator\"")]
        [InlineData("Text with \"quotes\"", "\"Text with \"\"quotes\"\"\"")]
        public void EscapeEncloseLineBreaks(string actual, string expected)
        {
            Assert.Equal(expected, CsvHelper.Escape(actual, ";"));
        }

        [Theory]
        [InlineData("Text with || separator", "\"Text with || separator\"")]
        [InlineData("Text with | separator", "Text with | separator")]
        public void EscapeEncloseComplexSeparators(string actual, string expected)
        {
            Assert.Equal(expected, CsvHelper.Escape(actual, "||"));
        }
    }
}