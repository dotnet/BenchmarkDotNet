using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Tests.XUnit;
using JetBrains.Annotations;
using System.Reflection;

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
        public void GetDisambiguatedDisplayNamesQualifiesCollidingNamesWithNamespace()
        {
            var types = new[]
            {
                typeof(Foo.Fixture),
                typeof(Bar.Fixture),
                typeof(ReflectionTests),
            };

            var displayNames = types.GetDisambiguatedDisplayNames();

            Assert.Equal("BenchmarkDotNet.Tests.Foo.Fixture", displayNames[0]);
            Assert.Equal("BenchmarkDotNet.Tests.Bar.Fixture", displayNames[1]);
            Assert.Equal("ReflectionTests", displayNames[2]);
        }

        [Fact]
        public void GetDisambiguatedDisplayNamesLeavesUniqueNamesUnqualified()
        {
            var types = new[] { typeof(Foo.Fixture), typeof(ReflectionTests) };

            var displayNames = types.GetDisambiguatedDisplayNames();

            Assert.Equal("Fixture", displayNames[0]);
            Assert.Equal("ReflectionTests", displayNames[1]);
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
        // The declared-type and cross-kind assertions below all fail without their part of the lookup. The
        // same-kind most-derived ones cannot: reflection is documented as returning members in no particular
        // order, but every runtime tested yields the derived member first, so a first match passes here too.
        // Those are asserted to pin the intent, not because this test can catch their absence.
        [Fact]
        public void GetParameterMemberPrefersTheMostDerivedOfAHiddenPair()
        {
            const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            const BindingFlags Static = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

            Assert.Equal(typeof(HidingMembers), typeof(HidingMembers).GetParameterMember("Field", typeof(string), Instance)?.DeclaringType);
            Assert.Equal(typeof(HidingMembers), typeof(HidingMembers).GetParameterMember("StaticField", typeof(string), Static)?.DeclaringType);

            // A field hiding a property and a property hiding a field: looking either kind up first finds the base
            // member of that kind, which is not the parameter.
            Assert.IsAssignableFrom<FieldInfo>(typeof(HidingMembers).GetParameterMember("PropertyHiddenByField", typeof(string), Instance));
            Assert.IsAssignableFrom<FieldInfo>(typeof(HidingMembers).GetParameterMember("StaticPropertyHiddenByField", typeof(string), Static));
            Assert.IsAssignableFrom<PropertyInfo>(typeof(HidingMembers).GetParameterMember("FieldHiddenByProperty", typeof(string), Instance));

            // The declared type still selects: only the base declares an int of that name.
            Assert.Equal(typeof(HiddenMembers), typeof(HidingMembers).GetParameterMember("Typed", typeof(int), Instance)?.DeclaringType);
            Assert.Equal(typeof(HidingMembers), typeof(HidingMembers).GetParameterMember("Typed", typeof(string), Instance)?.DeclaringType);

            // An indexer takes arguments and is never a parameter member.
            Assert.Null(typeof(HidingMembers).GetParameterMember("Item", typeof(string), Instance));
            Assert.Null(typeof(HidingMembers).GetParameterMember("Field", typeof(int), Instance));
        }

        public class HiddenMembers
        {
            public string Field = "";
            public static string StaticField = "";
            public int Typed;
            public string PropertyHiddenByField { get; set; } = "";
            public static string StaticPropertyHiddenByField { get; set; } = "";
            public string FieldHiddenByProperty = "";
        }

        public class HidingMembers : HiddenMembers
        {
            public new string Field = "";
            public static new string StaticField = "";
            public new string Typed = "";
            public new string PropertyHiddenByField = "";
            public static new string StaticPropertyHiddenByField = "";
            public new string FieldHiddenByProperty { get; set; } = "";

            public string this[int index] => "";
        }
    }
}

namespace BenchmarkDotNet.Tests.Foo
{
    public class Fixture { }
}

namespace BenchmarkDotNet.Tests.Bar
{
    public class Fixture { }
}