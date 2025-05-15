using BenchmarkDotNet.Jobs;
using System;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class MsBuildArgumentTests
    {
        [Theory]
        [InlineData("/p:CustomProperty=100%", "100%")]               // Contains percentage
        [InlineData("/p:CustomProperty=AAA;BBB", "AAA;BBB")]         // Contains semicolon
        [InlineData("/p:CustomProperty=AAA,BBB", "AAA,BBB")]         // Contains comma
        [InlineData("/p:CustomProperty=AAA BBB", "AAA BBB")]         // Contains space
        [InlineData("/p:CustomProperty=AAA%3BBBB", "AAA%3BBBB")]     // Contains escaped semicolon
        [InlineData("/p:CustomProperty=\"AAA%3BBBB\"", "AAA%3BBBB")] // Contains escaped semicolon. and surrounded with non-escaped double quote
        [InlineData("/p:NoWarn=1591;1573;3001", "1591;1573;3001")]   // NoWarn property that contains semicolon
        public void MSBuildArgument_ContainsSpecialChars(string argument, string expected)
        {
            var result = new MsBuildArgument(argument).GetEscapedTextRepresentation();
            AssertEscapedValue(result, expected);
        }

        [Theory]
        [InlineData("/p:CustomProperty=AAA_BBB")]                   // Argument that don't contains special chars
        [InlineData("\"/p:CustomProperty=AAA%3BBBB\"")]             // Argument is surrounded with double quote and value is escaped
        [InlineData("/p:CustomProperty=\\\"100%3B200\\\"")]         // Value is surrounded with double quote and value is escaped (For Windows environment)
        [InlineData("/p:CustomProperty=\\\'\\\"100%3B200\\\"\\\'")] // Value is surrounded with double quote and value is escaped (For non-Windows environment)
        [InlineData("/noWarn:1591;1573;3001")]                      // Other argument should not be escaped
        public void MSBuildArgument_ShouldNotBeEscaped(string originalArg)
        {
            var result = new MsBuildArgument(originalArg).GetEscapedTextRepresentation();
            Assert.Equal(expected: originalArg, result);
        }

        private static void AssertEscapedValue(string escapedArgument, string expectedValue)
        {
            var values = escapedArgument.Split(['='], 2);
            Assert.Equal(2, values.Length);

            var key = values[0];
            var value = values[1];

#if NET
            // On non-Windows environment value should be surrounded with escaped `'"` and `"'`
            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal("\\\'\\\"" + expectedValue + "\\\"\\\'", value);
                return;
            }
#endif
            // On Windows environment. Value should be quote with escaped `\"`
            Assert.Equal("\\\"" + expectedValue + "\\\"", value);
        }
    }
}
