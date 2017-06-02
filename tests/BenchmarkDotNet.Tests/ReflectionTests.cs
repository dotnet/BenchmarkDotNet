using System;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ReflectionTests
    {
        [Fact]
        public void GetCorrectTypeNameReturnCSharpFriendlyTypeName()
        {
            CheckCorrectTypeName("System.Int32", typeof(int));
            CheckCorrectTypeName("System.Int32[]", typeof(int[]));
            CheckCorrectTypeName("System.Int32[,]", typeof(int[,]));
            CheckCorrectTypeName("System.Tuple<System.Int32, System.Int32>[]", typeof(Tuple<int, int>[]));
            CheckCorrectTypeName("void", typeof(void));
            CheckCorrectTypeName("System.IEquatable<T>", typeof(IEquatable<>));
            CheckCorrectTypeName("BenchmarkDotNet.Tests.ReflectionTests.NestedNonGeneric1.NestedNonGeneric2", typeof(NestedNonGeneric1.NestedNonGeneric2));
            CheckCorrectTypeName("BenchmarkDotNet.Tests.ReflectionTests.NestedNonGeneric1.NestedGeneric2<System.Int16, System.Boolean, System.Decimal>", typeof(NestedNonGeneric1.NestedGeneric2<short, bool, decimal>));
        }

        [AssertionMethod]
        private static void CheckCorrectTypeName(string name, Type type)
        {
            Assert.Equal(name, type.GetCorrectTypeName());
        }

        public class NestedNonGeneric1
        {
            public class NestedNonGeneric2 { }
            public class NestedGeneric2<TA, TB, TC> { }
        }
    }
}