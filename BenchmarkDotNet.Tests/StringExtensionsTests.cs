using System.IO;
using BenchmarkDotNet.Extensions;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void AsValidPathReplacesAllInvalidFolderPathNameCharactersWithTheirRepresentation()
        {
            foreach (var invalidPathChar in Path.GetInvalidPathChars())
            {
                Assert.Equal($"char{(short)invalidPathChar}", invalidPathChar.ToString().AsValidPath());
            }
        }
    }
}