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
        [InlineData("/p:CustomProperty=AAA BBB", "AAA%20BBB")]       // Contains space (It's escaped to `%20`)
        [InlineData("/p:CustomProperty=AAA\"BBB", "AAA%22BBB")]      // Contains double quote (It's escaped to `%22B`)
        [InlineData("/p:CustomProperty=AAA\\BBB", "AAA%5CBBB")]      // Contains backslash (It's escaped to `%5C`)
        [InlineData("/p:CustomProperty=AAA%3BBBB", "AAA%3BBBB")]     // Contains escaped semicolon (`%3B`)
        [InlineData("/p:CustomProperty=\"AAA%3BBBB\"", "AAA%3BBBB")] // Contains escaped semicolon (`%3B`), and surrounded with double quote
        [InlineData("/p:NoWarn=1591;1573;3001", "1591;1573;3001")]   // NoWarn property that contains semicolon
        public void MSBuildArgument_ContainsMSBuildSpecialChars(string argument, string expected)
        {
            // Arrange
            var msbuildArgument = new MsBuildArgument(argument, escape: true);

            // Act
            var result = msbuildArgument.GetEscapedTextRepresentation();

            // Assert
            AssertEscapedValue(expected, result);

            // Helper method
            static void AssertEscapedValue(string expectedValue, string argument)
            {
                var values = argument.Split(['='], 2);
                Assert.Equal(2, values.Length);

                var key = values[0];
                var value = values[1];

                // Assert value is wrapped with `\"`
                Assert.StartsWith("\\\"", value);
                Assert.EndsWith("\\\"", value);

                value = value.Substring(2, value.Length - 4);
                Assert.Equal(expectedValue, value);
            }
        }

        [Theory]
        [InlineData("/p:CustomProperty=AAA_BBB")]           // Argument that don't contains special chars
        [InlineData("\"/p:CustomProperty=AAA%3BBBB\"")]     // Entire argument is surrounded with double quote and value is escaped
        [InlineData("/p:CustomProperty=\\\"100%3B200\\\"")] // Value is surrounded with escaped double quote and value is escaped
        [InlineData("/noWarn:1591;1573;3001")]              // Other argument that don't contains `=` should not be escaped
        public void MSBuildArgument_ShouldNotBeEscaped(string argument)
        {
            // Arrange
            var msbuildArgument = new MsBuildArgument(argument, escape: true);

            // Act
            var result = msbuildArgument.GetEscapedTextRepresentation();

            // Assert
            Assert.Equal(argument, result);
        }
    }
}
