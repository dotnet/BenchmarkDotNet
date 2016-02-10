using System;
using BenchmarkDotNet.Extensions;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ReflectionTests
    {
        [Fact]
        public void GetCorrectTypeNameTest()
        {
            Assert.Equal("System.Int32", typeof(int).GetCorrectTypeName());
            Assert.Equal("System.Int32[]", typeof(int[]).GetCorrectTypeName());
            Assert.Equal("System.Int32[,]", typeof(int[,]).GetCorrectTypeName());
            Assert.Equal("System.Tuple<System.Int32, System.Int32>[]", typeof(Tuple<int, int>[]).GetCorrectTypeName());
        }
    }
}