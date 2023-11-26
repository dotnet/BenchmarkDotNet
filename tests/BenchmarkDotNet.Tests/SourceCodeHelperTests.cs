using System;
using System.Reflection;
using BenchmarkDotNet.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    // TODO: add decimal, typeof, CreateInstance, TimeValue, IntPtr, IFormattable
    public class SourceCodeHelperTests
    {
        private readonly ITestOutputHelper output;

        public SourceCodeHelperTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [InlineData(null, "null")]
        [InlineData(false, "false")]
        [InlineData(true, "true")]
        [InlineData("string", "\"string\"")]
        [InlineData("string/\\", @"""string/\\""")]
        [InlineData('a', "'a'")]
        [InlineData('\\', "'\\\\'")]
        [InlineData(0.123f, "0.123f")]
        [InlineData(0.123d, "0.123d")]
        [InlineData(BindingFlags.Public, "(System.Reflection.BindingFlags)(16)")]
        public void ToSourceCodeSimpleTest(object? original, string expected)
        {
            string actual = SourceCodeHelper.ToSourceCode(original);
            output.WriteLine("ORIGINAL  : " + original + " (" + original?.GetType() + ")");
            output.WriteLine("ACTUAL    : " + actual);
            output.WriteLine("EXPECTED  : " + expected);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SupportsGuid()
        {
            const string guidAsString = "e9a42b02-d5df-448d-aa00-03f14749eb61";
            Assert.Equal($"System.Guid.Parse(\"{guidAsString}\")", SourceCodeHelper.ToSourceCode(Guid.Parse(guidAsString)));
        }

        [Fact]
        public void CanEscapeJson()
        {
            const string expected = "\"{ \\\"message\\\": \\\"Hello, World!\\\" }\"";

            var actual = SourceCodeHelper.ToSourceCode("{ \"message\": \"Hello, World!\" }");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanEscapePath()
        {
            const string expected = @"""C:\\Projects\\BenchmarkDotNet\\samples\\BenchmarkDotNet.Samples""";

            var actual = SourceCodeHelper.ToSourceCode(@"C:\Projects\BenchmarkDotNet\samples\BenchmarkDotNet.Samples");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanEscapeControlCharacters()
        {
            const string expected = @""" \0 \b \f \n \t \v \"" a a a a { } """;

            var actual = SourceCodeHelper.ToSourceCode(" \0 \b \f \n \t \v \" \u0061 \x0061 \x61 \U00000061 { } ");

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData('\0', @"'\0'")]
        [InlineData('\b', @"'\b'")]
        [InlineData('\f', @"'\f'")]
        [InlineData('\n', @"'\n'")]
        [InlineData('\t', @"'\t'")]
        [InlineData('\v', @"'\v'")]
        [InlineData('\'', @"'\''")]
        [InlineData('\u0061', "'a'")]
        [InlineData('"', "'\"'")]
        [InlineData('{', "'{'")]
        [InlineData('}', "'}'")]
        public void CanEscapeControlCharactersInChar(char original, string excepted)
        {
            var actual = SourceCodeHelper.ToSourceCode(original);

            Assert.Equal(excepted, actual);
        }
    }
}