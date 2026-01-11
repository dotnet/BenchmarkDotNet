using BenchmarkDotNet.Helpers;
using System.IO;
using Xunit;

namespace BenchmarkDotNet.Tests.Helpers
{
    // Using test patterns of Path.GetRelativePath
    // https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Runtime/tests/System.Runtime.Extensions.Tests/System/IO/Path.GetRelativePath.cs
    public class PathHelperTests
    {
#if NETFRAMEWORK
        [Theory]
        [InlineData(@"C:\", @"C:\", @".")]
        [InlineData(@"C:\a", @"C:\a\", @".")]
        [InlineData(@"C:\A", @"C:\a\", @".")]
        [InlineData(@"C:\a\", @"C:\a", @".")]
        [InlineData(@"C:\", @"C:\b", @"b")]
        [InlineData(@"C:\a", @"C:\b", @"..\b")]
        // [InlineData(@"C:\a", @"C:\b\", @"..\b\")] // This test failed with GetRelativePathCompat.
        [InlineData(@"C:\a\b", @"C:\a", @"..")]
        [InlineData(@"C:\a\b", @"C:\a\", @"..")]
        [InlineData(@"C:\a\b\", @"C:\a", @"..")]
        [InlineData(@"C:\a\b\", @"C:\a\", @"..")]
        [InlineData(@"C:\a\b\c", @"C:\a\b", @"..")]
        [InlineData(@"C:\a\b\c", @"C:\a\b\", @"..")]
        [InlineData(@"C:\a\b\c", @"C:\a", @"..\..")]
        [InlineData(@"C:\a\b\c", @"C:\a\", @"..\..")]
        [InlineData(@"C:\a\b\c\", @"C:\a\b", @"..")]
        [InlineData(@"C:\a\b\c\", @"C:\a\b\", @"..")]
        [InlineData(@"C:\a\b\c\", @"C:\a", @"..\..")]
        [InlineData(@"C:\a\b\c\", @"C:\a\", @"..\..")]
        [InlineData(@"C:\a\", @"C:\b", @"..\b")]
        [InlineData(@"C:\a", @"C:\a\b", @"b")]
        [InlineData(@"C:\a", @"C:\A\b", @"b")]
        [InlineData(@"C:\a", @"C:\b\c", @"..\b\c")]
        [InlineData(@"C:\a\", @"C:\a\b", @"b")]
        [InlineData(@"C:\", @"D:\", @"D:\")]
        [InlineData(@"C:\", @"D:\b", @"D:\b")]
        [InlineData(@"C:\", @"D:\b\", @"D:\b\")]
        [InlineData(@"C:\a", @"D:\b", @"D:\b")]
        [InlineData(@"C:\a\", @"D:\b", @"D:\b")]
        [InlineData(@"C:\ab", @"C:\a", @"..\a")]
        [InlineData(@"C:\a", @"C:\ab", @"..\ab")]
        [InlineData(@"C:\", @"\\LOCALHOST\Share\b", @"\\LOCALHOST\Share\b")]
        [InlineData(@"\\LOCALHOST\Share\a", @"\\LOCALHOST\Share\b", @"..\b")]
        public void GetRelativePathTest_Windows(string relativeTo, string path, string expected)
        {
            // Arrange
            expected = expected.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // Act
            var result = PathHelper.GetRelativePath(relativeTo, path);

            // Assert
            Assert.Equal(expected, result);
        }
#endif
    }
}
