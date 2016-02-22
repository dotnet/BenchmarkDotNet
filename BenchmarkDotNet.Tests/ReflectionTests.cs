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
            CheckCorrectTypeName("System.Int32", typeof(int));
            CheckCorrectTypeName("System.Int32[]", typeof(int[]));
            CheckCorrectTypeName("System.Int32[,]", typeof(int[,]));
            CheckCorrectTypeName("System.Tuple<System.Int32, System.Int32>[]", typeof(Tuple<int, int>[]));
            CheckCorrectTypeName("void", typeof(void));
            CheckCorrectTypeName("System.IEquatable<T>", typeof(IEquatable<>));
        }

        private static void CheckCorrectTypeName(string name, Type type)
        {
            Assert.Equal(name, type.GetCorrectTypeName());
        }
    }
}