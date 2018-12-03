using System.Collections.Generic;
using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class FolderNameTests
    {
        [Theory]
        [InlineData(typeof(FolderNameTests), "BenchmarkDotNet.Tests.FolderNameTests")]
        [InlineData(typeof(List<int>), "System.Collections.Generic.List_Int32_")]
        public void FileNamesAreConsistentAcrossOSes(object value, string expectedName)
        {
            Assert.Equal(expectedName, FolderNameHelper.ToFolderName(value));
        }
    }
}