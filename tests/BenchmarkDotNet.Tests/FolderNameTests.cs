using System.Collections.Generic;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class FolderNameTests
    {
        [Theory]
        [InlineData(false, "false")]
        [InlineData(true, "true")]
        [InlineData("<MyString>", "_MyString_")]
        [InlineData("TestCase:Arg", "TestCase_Arg")]
        [InlineData('a', "97")]
        [InlineData(0.42f, "0-42")]
        [InlineData(0.42d, "0-42")]
        [InlineData(Jit.RyuJit, "RyuJit")]
        [InlineData(typeof(int), "System.Int32")]
        public void ToFolderNameTest(object value, string expectedName)
        {
            Assert.Equal(expectedName, FolderNameHelper.ToFolderName(value));
        }

        // Value types can't be used as attribute arguments
        [Fact]
        public void ToFolderNameStructTest()
        {
            Assert.Equal("0-42", FolderNameHelper.ToFolderName(0.42m));
            Assert.Equal("1234000000ns", FolderNameHelper.ToFolderName(TimeInterval.FromSeconds(1.234)));
        }

        [Theory]
        [InlineData(typeof(FolderNameTests), "BenchmarkDotNet.Tests.FolderNameTests")]
        [InlineData(typeof(List<int>), "System.Collections.Generic.List_Int32_")]
        public void FileNamesAreConsistentAcrossOSes(object value, string expectedName)
        {
            Assert.Equal(expectedName, FolderNameHelper.ToFolderName(value));
        }
    }
}