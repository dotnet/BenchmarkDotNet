using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Tests.XUnit;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ReflectionTests
    {
        [Fact]
        public void GetCorrectCSharpTypeNameReturnsCSharpFriendlyTypeName()
        {
            CheckCorrectTypeName("global::System.Int32", typeof(int));
            CheckCorrectTypeName("global::System.Int32[]", typeof(int[]));
            CheckCorrectTypeName("global::System.Int32[][]", typeof(int[][]));
            CheckCorrectTypeName("global::System.Int32[,]", typeof(int[,]));
            CheckCorrectTypeName("global::System.Tuple<global::System.Int16, global::System.Int32>[]", typeof(Tuple<short, int>[]));
            CheckCorrectTypeName("global::System.ValueTuple<global::System.Int16, global::System.Int32>[]", typeof(ValueTuple<short, int>[]));
            CheckCorrectTypeName("void", typeof(void));
            CheckCorrectTypeName("void*", typeof(void*));
            CheckCorrectTypeName("global::System.IEquatable<T>", typeof(IEquatable<>));
            CheckCorrectTypeName("global::System.Type", typeof(Type));
            // ReSharper disable once PossibleMistakenCallToGetType.2
            CheckCorrectTypeName("global::System.Reflection.TypeInfo", typeof(string).GetType()); // typeof(string).GetType() == System.RuntimeType which is not public
        }

        [Fact]
        public void GetCorrectCSharpTypeNameSupportsGenericTypesPassedByReference()
        {
            var byRefGenericType = typeof(GenericByRef).GetMethod(nameof(GenericByRef.TheMethod))!.GetParameters().Single().ParameterType;

            CheckCorrectTypeName("global::System.ValueTuple<global::System.Int32, global::System.Int16>", byRefGenericType);
        }

        public class GenericByRef
        {
            public void TheMethod(ref (int, short) _) { }
        }

        [Fact]
        public void GetCorrectCSharpTypeNameSupportsNestedTypes()
        {
            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.Nested", typeof(Nested));

            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedNonGeneric1.NestedNonGeneric2",
                typeof(NestedNonGeneric1.NestedNonGeneric2));
            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedNonGeneric1.NestedGeneric2<global::System.Int16, global::System.Boolean, global::System.Decimal>",
                typeof(NestedNonGeneric1.NestedGeneric2<short, bool, decimal>));

            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedNonGeneric1.NestedGeneric2<global::System.Int16, global::System.Boolean, global::System.Decimal>.NestedNonGeneric3",
                typeof(NestedNonGeneric1.NestedGeneric2<short, bool, decimal>.NestedNonGeneric3));

            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedGeneric1<global::System.Byte, global::System.SByte>",
                typeof(NestedGeneric1<byte, sbyte>));
            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedGeneric1<global::System.Byte, global::System.SByte>.NonGeneric2",
                typeof(NestedGeneric1<byte, sbyte>.NonGeneric2));
            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedGeneric1<global::System.Byte, global::System.SByte>.NonGeneric2.Generic3<global::System.Int16, global::System.Int32, global::System.Int64>",
                typeof(NestedGeneric1<byte, sbyte>.NonGeneric2.Generic3<short, int, long>));
            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedGeneric1<global::System.Byte, global::System.SByte>.NonGeneric2.Generic3<global::System.Int16, global::System.Int32, global::System.Int64>.NonGeneric4",
                typeof(NestedGeneric1<byte, sbyte>.NonGeneric2.Generic3<short, int, long>.NonGeneric4));
            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedGeneric1<global::System.Byte, global::System.SByte>.NonGeneric2.Generic3<global::System.Int16, global::System.Int32, global::System.Int64>.Generic4<global::System.Single, global::System.Double>",
                typeof(NestedGeneric1<byte, sbyte>.NonGeneric2.Generic3<short, int, long>.Generic4<float, double>));

            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedGeneric1<T1, T2>",
                typeof(NestedGeneric1<,>));
            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedGeneric1<T1, T2>.NonGeneric2.Generic3<V1, V2, V3>.NonGeneric4",
                typeof(NestedGeneric1<,>.NonGeneric2.Generic3<,,>.NonGeneric4));
            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.NestedGeneric1<T1, T2>.NonGeneric2.Generic3<V1, V2, V3>.Generic4<W1, W2>",
                typeof(NestedGeneric1<,>.NonGeneric2.Generic3<,,>.Generic4<,>));
        }

        [Fact]
        public void GetCorrectCSharpTypeNameSupportsNestedTypesPassedByReference()
        {
            var byRefNestedType = typeof(Nested).GetMethod(nameof(Nested.TheMethod))!.GetParameters().Single().ParameterType;

            CheckCorrectTypeName("global::BenchmarkDotNet.Tests.ReflectionTests.Nested", byRefNestedType);
        }

        public class Nested
        {
            public void TheMethod(ref Nested _) { }
        }

        public class NestedNonGeneric1
        {
            public class NestedNonGeneric2 { }

            public class NestedGeneric2<TA, TB, TC>
            {
                public class NestedNonGeneric3 { }
            }
        }

        public class NestedGeneric1<T1, T2>
        {
            public class NonGeneric2
            {
                public class Generic3<V1, V2, V3>
                {
                    public class NonGeneric4 { }

                    public class Generic4<W1, W2> { }
                }
            }
        }

        [AssertionMethod]
        private static void CheckCorrectTypeName(string expectedName, Type type)
        {
            Assert.Equal(expectedName, type.GetCorrectCSharpTypeName());
        }

        [Fact]
        public void GetDisplayNameReturnsTypeNameWithGenericArguments()
        {
            CheckCorrectDisplayName("Int32", typeof(int));
            CheckCorrectDisplayName("List<Int32>", typeof(List<int>));
            CheckCorrectDisplayName("List<ReflectionTests>", typeof(List<ReflectionTests>));
        }

        [AssertionMethod]
        private static void CheckCorrectDisplayName(string expectedName, Type type)
        {
            Assert.Equal(expectedName, type.GetDisplayName());
        }

        [Fact]
        public void OnlyClosedGenericsWithPublicParameterlessCtorsAreSupported()
        {
            Assert.False(typeof(Generic<>).ContainsRunnableBenchmarks());
            Assert.False(typeof(GenericNoPublicCtor<>).ContainsRunnableBenchmarks());
            Assert.False(typeof(GenericNoPublicCtor<int>).ContainsRunnableBenchmarks());

            Assert.True(typeof(Generic<int>).ContainsRunnableBenchmarks());
        }

        public class Generic<T>
        {
            [Benchmark] public T Create() => default!;
        }

        public class GenericNoPublicCtor<T>
        {
            private GenericNoPublicCtor() { }

            [Benchmark] public T Create() => default!;
        }

        [FactEnvSpecific("The implicit cast operator is available only in .NET Core 2.1+ (See https://github.com/dotnet/corefx/issues/30121 for more)",
            EnvRequirement.DotNetCoreOnly)]
        public void StringCanBeUsedAsReadOnlySpanOfCharArgument() => Assert.True(typeof(ReadOnlySpan<char>).IsStackOnlyWithImplicitCast("a string"));

        [Fact]
        public void StackOnlyTypesWithImplicitCastOperatorAreSupportedAsArguments()
        {
            Assert.True(typeof(Span<byte>).IsStackOnlyWithImplicitCast(new byte[] { 1, 2, 3 }));
            Assert.True(typeof(StackOnlyStruct<byte>).IsStackOnlyWithImplicitCast(new WithImplicitCastToStackOnlyStruct<byte>() { Array = [] }));

            Assert.False(typeof(StackOnlyStruct<byte>).IsStackOnlyWithImplicitCast(new WithImplicitCastToStackOnlyStruct<bool>() { Array = [] })); // different T

            Assert.False(typeof(List<byte>).IsStackOnlyWithImplicitCast(new byte[] { 1, 3, 3 }));
        }

        public ref struct StackOnlyStruct<T>
        {
            public Span<T> Span;
        }

        public class WithImplicitCastToStackOnlyStruct<T>
        {
            public required T[] Array;

            public static implicit operator StackOnlyStruct<T>(WithImplicitCastToStackOnlyStruct<T> instance)
                => new StackOnlyStruct<T> { Span = instance.Array };
        }
    }
}