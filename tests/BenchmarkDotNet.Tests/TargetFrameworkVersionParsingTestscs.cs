using BenchmarkDotNet.Disassemblers;
using System;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class TargetFrameworkVersionParsingTestscs
    {
        [Theory]
        [InlineData("net461", 4, 6, 1)]
        [InlineData("net48", 4, 8, -1)]
        [InlineData("net7.0", 7, 0, -1)]
        [InlineData("net7.0-windows8", 7, 0, -1)]
        public void RuntimeVersionCanBeParsedFromTfm(string tfm, int major, int minor, int build)
        {
            Version version = State.ParseVersion(tfm);

            Assert.Equal(major, version.Major);
            Assert.Equal(minor, version.Minor);
            Assert.Equal(build, version.Build);
        }
    }
}
