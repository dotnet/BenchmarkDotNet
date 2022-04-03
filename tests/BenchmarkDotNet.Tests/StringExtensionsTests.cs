using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Extensions;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void AsValidFileNameReplacesAllInvalidFileNameCharactersWithTheirRepresentation()
        {
            foreach (char invalidPathChar in Path.GetInvalidFileNameChars())
                Assert.Equal($"char{(short) invalidPathChar}", invalidPathChar.ToString().AsValidFileName());
        }

        [Fact]
        public void AsValidFileNameDoesNotChangeValidFileNames()
        {
            const string validFileName = "valid_File-Name.exe";

            Assert.Equal(validFileName, validFileName.AsValidFileName());
        }

        [Fact]
        public void HtmlEncodeCharacters()
        {
            string expectedHtml = "&lt;&#39;&gt;&quot;&amp;shouldntchange";
            string html = "<'>\"&shouldntchange";

            Assert.Equal(expectedHtml, html.HtmlEncode());
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData(" ", "")]
        [InlineData("-n win-x64", " -n win-x64")]
        [InlineData(" -n win-x64", " -n win-x64")]
        [InlineData("-n win-x64 ", " -n win-x64")]
        [InlineData(" -n Win-x64 ", " -n Win-x64")]
        [InlineData(" a ", " a")]
        [InlineData("        a        ", " a")]
        [InlineData("   \r\n  a   \r\n", " a")]
        public void AppendArgumentMakesSureOneSpaceBeforeStringArgument(string input, string expectedOutput)
        {
            var stringBuilder = new StringBuilder();
            var result = stringBuilder.AppendArgument(input).ToString();

            Assert.Equal(expectedOutput, result);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("http://test.com/", " http://test.com/")]
        [InlineData(" http://test.com/", " http://test.com/")]
        [InlineData("http://test.com/ ", " http://test.com/")]
        [InlineData(" http://test.com/ ", " http://test.com/")]
        [InlineData("\r\n  http://test.com/  \r\n", " http://test.com/")]
        public void AppendArgumentMakesSureOneSpaceBeforeObjectArgument(string input, string expectedOutput)
        {
            Uri uri = input != null ? new Uri(input) : null; // Use Uri for our object type since that is what is used in code
            var stringBuilder = new StringBuilder();
            var result = stringBuilder.AppendArgument(uri).ToString();

            Assert.Equal(expectedOutput, result);
        }
    }
}