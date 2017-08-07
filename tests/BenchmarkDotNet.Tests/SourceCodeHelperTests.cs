using System.Reflection;
using BenchmarkDotNet.Core.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    // TODO: add decimal, typeof, CreateInstance, TimeInterval, IntPtr, IFormattable
    public class SourceCodeHelperTests
    {
        private ITestOutputHelper output;

        public SourceCodeHelperTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(null, "null")]
        [InlineData(false, "false")]
        [InlineData(true, "true")]
        [InlineData("string", "\"string\"")]
        [InlineData("string/\\", "\"string/\\\\\"")]
        [InlineData('a', "'a'")]
        [InlineData('\\', "'\\\\'")]
        [InlineData(0.123f, "0.123f")]
        [InlineData(0.123d, "0.123d")]        
        [InlineData(BindingFlags.Public, "System.Reflection.BindingFlags.Public")]        
        public void ToSourceCodeSimpleTest(object original, string expected)
        {
            string actual = SourceCodeHelper.ToSourceCode(original);
            output.WriteLine("ORIGINAL  : " + original + " (" + original?.GetType() + ")");
            output.WriteLine("ACTUAL    : " + actual);
            output.WriteLine("EXPRECTED : " + expected);
            Assert.Equal(expected, actual);
        }
    }
}