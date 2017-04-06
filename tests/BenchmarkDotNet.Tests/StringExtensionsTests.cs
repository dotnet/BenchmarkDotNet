using System.IO;
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
    }
}