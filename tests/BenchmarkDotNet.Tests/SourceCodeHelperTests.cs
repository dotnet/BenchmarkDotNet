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
        [InlineData("string", "$\"string\"")]
        [InlineData("string/\\", @"$""string/\\""")]
        [InlineData('a', "'a'")]
        [InlineData('\\', "'\\\\'")]
        [InlineData(0.123f, "0.123f")]
        [InlineData(0.123d, "0.123d")]
        [InlineData(BindingFlags.Public, "(System.Reflection.BindingFlags)(16)")]
        public void ToSourceCodeSimpleTest(object original, string expected)
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
            const string expected = "$\"{{ \\\"message\\\": \\\"Hello, World!\\\" }}\"";

            var actual = SourceCodeHelper.ToSourceCode("{ \"message\": \"Hello, World!\" }");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanEscapePath()
        {
            const string expected = @"$""C:\\Projects\\BenchmarkDotNet\\samples\\BenchmarkDotNet.Samples""";

            var actual = SourceCodeHelper.ToSourceCode(@"C:\Projects\BenchmarkDotNet\samples\BenchmarkDotNet.Samples");

            Assert.Equal(expected, actual);
        }
    }
}