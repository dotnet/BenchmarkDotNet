using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

// Several fixtures below declare the very shapes these rules refuse - that is what the tests assert, through the
// declaration errors discovery reports for them. Disabled for the file rather than around each one: restoring
// between them would be noise when so many of them exist to be refused, and the assertions on the messages are
// what would catch a rule that stopped firing.
#pragma warning disable BDN1506 // [ArgumentsSource] must match the benchmark's parameters for every type argument
#pragma warning disable BDN1507 // [ArgumentsSource] must yield what the benchmark's parameters take

namespace BenchmarkDotNet.Tests
{
    public class ParamsSourceTests
    {
        // #1809
        [Fact]
        public void NullIsSupportedAsElementOfParamsSource()
        {
            BenchmarkConverter.TypeToBenchmarks(typeof(ParamsSourceWithNull));
        }

        public class ParamsSourceWithNull
        {
            public static IEnumerable<object?> Values()
            {
                yield return null;
                yield return ValueTuple.Create(10);
                yield return (10, 20);
                yield return (10, 20, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            }

            [ParamsSource(nameof(Values))]
            public required object? O { get; set; }

            [Benchmark]
            public object? FooBar() => O;
        }

        [Fact]
        public void AsyncEnumerableNullParamsSourceIsResolvedAtDiscovery()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableNullParams)).BenchmarksCases;

            var values = benchmarks
                .Select(b => b.Parameters.Items.Single(p => p.Name == nameof(AsyncEnumerableNullParams.Value)).Value)
                .ToArray();

            Assert.Equal(new object?[] { null, "x" }, values);
        }

        public class AsyncEnumerableNullParams
        {
            public static async IAsyncEnumerable<object?> Values()
            {
                await Task.Yield();
                yield return null;
                yield return "x";
            }

            [ParamsSource(nameof(Values))]
            public object? Value { get; set; }

            [Benchmark]
            public object? Run() => Value;
        }

        [Fact]
        public void AsyncEnumerableNullArgumentsSourceIsResolvedAtDiscovery()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableNullArguments)).BenchmarksCases;

            var values = benchmarks
                .Select(b => b.Parameters.Items.Single().Value)
                .ToArray();

            Assert.Equal(new object?[] { null, "x" }, values);
        }

        public class AsyncEnumerableNullArguments
        {
            public static async IAsyncEnumerable<object?> Arguments()
            {
                await Task.Yield();
                yield return null;
                yield return "x";
            }

            [Benchmark]
            [ArgumentsSource(nameof(Arguments))]
            public object? Run(object? argument) => argument;
        }

        // #2980
        [Fact]
        public void WriteOnlyPropertyDoesThrowNullReferenceException()
        {
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(ClassWithWriteOnlyProperty)).DeclarationErrors);

            Assert.Contains(nameof(ClassWithWriteOnlyProperty.WriteOnlyValues), error.Message);
            Assert.Contains("no public, accessible method/property", error.Message);
        }

        public class ClassWithWriteOnlyProperty
        {
            private int _writeOnlyValue;

            public int WriteOnlyValues
            {
                set { _writeOnlyValue = value; }
            }

#pragma warning disable BDN1305 // Test intentionally uses write-only property
            [ParamsSource(nameof(WriteOnlyValues))]
            public int MyParam { get; set; }
#pragma warning restore BDN1305

            [Benchmark]
            public void Run() { }
        }

        [Fact]
        public void AsyncEnumerableParamsSourceIsResolvedAtDiscovery()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableParams)).BenchmarksCases;

            var values = benchmarks
                .Select(b => b.Parameters.Items.Single(p => p.Name == nameof(AsyncEnumerableParams.Value)).Value)
                .ToArray();

            Assert.Equal(new object[] { 1, 2, 3 }, values);
        }

        public class AsyncEnumerableParams
        {
            public static async IAsyncEnumerable<object> Values()
            {
                await Task.Yield();
                yield return 1;
                yield return 2;
                yield return 3;
            }

            [ParamsSource(nameof(Values))]
            public int Value { get; set; }

            [Benchmark]
            public int Run() => Value;
        }

        [Fact]
        public void AsyncEnumerableValueTypeParamsSourceIsResolvedAtDiscovery()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableValueTypeParams)).BenchmarksCases;

            var values = benchmarks
                .Select(b => b.Parameters.Items.Single(p => p.Name == nameof(AsyncEnumerableValueTypeParams.Value)).Value)
                .ToArray();

            Assert.Equal(new object[] { 1, 2, 3 }, values);
        }

        public class AsyncEnumerableValueTypeParams
        {
            public static async IAsyncEnumerable<int> Values()
            {
                await Task.Yield();
                yield return 1;
                yield return 2;
                yield return 3;
            }

            [ParamsSource(nameof(Values))]
            public int Value { get; set; }

            [Benchmark]
            public int Run() => Value;
        }

        [Fact]
        public void AsyncEnumerableSourceWithOptionalParametersIsResolvedAtDiscovery()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableOptionalParams)).BenchmarksCases;

            var values = benchmarks
                .Select(b => b.Parameters.Items.Single(p => p.Name == nameof(AsyncEnumerableOptionalParams.Value)).Value)
                .ToArray();

            Assert.Equal(new object[] { 1, 2 }, values);
        }

        public class AsyncEnumerableOptionalParams
        {
            public static async IAsyncEnumerable<object> Values(
                [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                yield return 1;
                yield return 2;
            }

            [ParamsSource(nameof(Values))]
            public int Value { get; set; }

            [Benchmark]
            public int Run() => Value;
        }

        [Fact]
        public void ParamsSourceWithOptionalParameterWithoutDefaultIsResolved()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(OptionalWithoutDefaultParams)).BenchmarksCases;

            var values = benchmarks
                .Select(b => b.Parameters.Items.Single(p => p.Name == nameof(OptionalWithoutDefaultParams.Value)).Value)
                .ToArray();

            Assert.Equal(new object[] { 0, 1 }, values);
        }

        public class OptionalWithoutDefaultParams
        {
            // Optional without a declared default: BDN must pass default(T), since Invoke does no optional binding.
            public static IEnumerable<object> Values([System.Runtime.InteropServices.Optional] int start)
            {
                yield return start;
                yield return start + 1;
            }

            [ParamsSource(nameof(Values))]
            public int Value { get; set; }

            [Benchmark]
            public int Run() => Value;
        }

        [Theory]
        // A reference-type element is read through IAsyncEnumerable<T>'s covariance and a value-type one through
        // reflection, which wraps whatever the source threw in a TargetInvocationException. What the user sees must
        // not depend on that: both surface the exception as thrown.
        [InlineData(typeof(ThrowingAsyncSource.OfReferenceType))]
        [InlineData(typeof(ThrowingAsyncSource.OfValueType))]
        public void AThrowingAsyncSourceSurfacesItsOwnException(Type benchmarkType)
        {
            var exception = Assert.Throws<InvalidTimeZoneException>(() => BenchmarkConverter.TypeToBenchmarks(benchmarkType));

            Assert.Equal("from the source", exception.Message);
        }

        public static class ThrowingAsyncSource
        {
            public class OfReferenceType
            {
                public static async IAsyncEnumerable<object> Values()
                {
                    await Task.Yield();
                    yield return default!;

                    // Discovery reads the whole sequence, so the next move reaches this.
                    throw new InvalidTimeZoneException("from the source");
                }

                [Benchmark][ArgumentsSource(nameof(Values))] public object Run(object a) => a;
            }

            public class OfValueType
            {
                public static async IAsyncEnumerable<int> Values()
                {
                    await Task.Yield();
                    yield return default!;

                    // Discovery reads the whole sequence, so the next move reaches this.
                    throw new InvalidTimeZoneException("from the source");
                }

                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(int a) => a;
            }
        }

        [Fact]
        public void OneArgumentTakingTheTypeParameterItselfIsAccepted()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(MatchedGenericArgumentSource<int>)).BenchmarksCases;

            var parameter = Assert.Single(Assert.Single(benchmarks).Parameters.Items);
            Assert.Equal(7, parameter.Value);
        }

        [Fact]
        public void OneArgumentTakingTheTypeParameterByImplicitConversionIsAccepted()
        {
            // ReadOnlySpan<byte> never accepts byte[] by assignability, only through its implicit conversion -
            // nothing is indexed, so the declaration holds whatever T is.
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(SpanFromGenericArgumentSource<byte[]>)).BenchmarksCases;

            Assert.Single(Assert.Single(benchmarks).Parameters.Items);
        }

        [Theory]
        [InlineData(typeof(WholeArrayByConversion.ToReadOnlySpan))]
        [InlineData(typeof(WholeArrayByConversion.ToRefArray))]
        public void AnArrayAParameterIsDeclaredAsOrBuiltFromIsNotAnArgumentList(Type benchmarkType)
        {
            var parameter = Assert.Single(Assert.Single(BenchmarkConverter.TypeToBenchmarks(benchmarkType).BenchmarksCases).Parameters.Items);

            Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<byte[]>(parameter.Value));
        }

        // Only a by-ref-like parameter is reached through a conversion operator, because nothing else can be. Every
        // other parameter names what it takes, so an element that merely converts to it is refused - the array here
        // is a byte[] and these parameters are not.
        [Theory]
        [InlineData(typeof(WholeArrayByConversion.ToReadOnlyMemory), "ReadOnlyMemory<Byte>")]
        [InlineData(typeof(WholeArrayByConversion.ToMemory), "Memory<Byte>")]
#if !NETFRAMEWORK
        [InlineData(typeof(WholeArrayByConversion.ToArraySegment), "ArraySegment<Byte>")]
#endif
        public void AnArrayIsRefusedWhereTheParameterOnlyConvertsFromIt(Type benchmarkType, string takes)
        {
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(benchmarkType).DeclarationErrors);

            Assert.Contains($"is declared to yield Byte[] for a {takes} argument", error.Message);
            Assert.Contains($"declare the source to yield {takes} or object", error.Message);
        }

        // A source declared to yield object says nothing about what it holds, and a null says nothing either, so
        // only the parameter is left to ask - and no value type has such a value. Refused where the declaration
        // is read rather than left to fail as a field the generated code cannot declare.
        [Fact]
        public void ANullIsRefusedForAByRefLikeParameterWhereTheSourceNamesOnlyObject()
        {
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(NullFromObjectToByRefLike)).DeclarationErrors);

            Assert.Contains("[ArgumentsSource(Values)] provides null for the ReadOnlySpan<Byte> parameter 'a'", error.Message);
            Assert.Contains("which is a value type - null is not one of its values", error.Message);
        }

        public class NullFromObjectToByRefLike
        {
            public static IEnumerable<object?> Values() { yield return null; }
            [Benchmark][ArgumentsSource(nameof(Values))] public int Run(ReadOnlySpan<byte> a) => a.Length;
        }

        // Nothing about that is particular to a ref struct: an int has no null either, and the two toolchains do
        // not agree about one - the generated code will not compile (CS0037) where the emitted one substitutes 0
        // and runs a case nobody wrote.
        [Fact]
        public void ANullIsRefusedForAValueTypeParameterWhereTheSourceNamesOnlyObject()
        {
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(NullFromObjectToValueType)).DeclarationErrors);

            Assert.Contains("[ArgumentsSource(Values)] provides null for the Int32 parameter 'a'", error.Message);
            Assert.Contains("which is a value type - null is not one of its values", error.Message);
        }

        public class NullFromObjectToValueType
        {
            public static IEnumerable<object?> Values() { yield return null; }
            [Benchmark][ArgumentsSource(nameof(Values))] public int Run(int a) => a;
        }

        // A reference type holds null perfectly well, so the same declaration is left alone.
        [Fact]
        public void ANullIsAcceptedForAReferenceTypeParameterWhereTheSourceNamesOnlyObject()
        {
            var parameter = Assert.Single(Assert.Single(
                BenchmarkConverter.TypeToBenchmarks(typeof(NullFromObjectToReferenceType)).BenchmarksCases).Parameters.Items);

            Assert.Null(parameter.Value);
        }

        public class NullFromObjectToReferenceType
        {
            public static IEnumerable<object?> Values() { yield return null; }
            [Benchmark][ArgumentsSource(nameof(Values))] public int Run(string? a) => a?.Length ?? 0;
        }

        // A source that names its element type has been judged on it already, and a null read from one says the
        // value could not be read rather than that it was null - a ref struct cannot be boxed to be looked at.
        [Fact]
        public void ANullIsLeftAloneWhereTheSourceNamesItsElementType()
            => Assert.NotEmpty(BenchmarkConverter.TypeToBenchmarks(typeof(NullFromTypedSource)).BenchmarksCases);

        public class NullFromTypedSource
        {
            public static IEnumerable<int?> Values() { yield return null; }
            [Benchmark][ArgumentsSource(nameof(Values))] public int Run(int? a) => a ?? 0;
        }

        // The same question, of the one place a value comes from with no declaration behind it at all. BDN1502
        // reports this where the benchmark is written; a host that runs no analyzer has only this.
        [Fact]
        public void ANullArgumentIsRefusedForAByRefLikeParameter()
        {
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(NullArgumentToByRefLike)).DeclarationErrors);

            Assert.Contains("[Arguments] on Run provides null for the ReadOnlySpan<Byte> parameter 'a'", error.Message);
            Assert.Contains("which is a value type - null is not one of its values", error.Message);
        }

        public class NullArgumentToByRefLike
        {
#pragma warning disable BDN1502 // the shape this refuses is the one the analyzer reports where it runs
            [Benchmark][Arguments(null)] public int Run(ReadOnlySpan<byte> a) => a.Length;
#pragma warning restore BDN1502
        }

        // The conversion is looked for on the type the source declares, which is what admitted the source in the
        // first place. Reading it off the value instead asks a different question - the operator is declared for
        // the base, and reflection matches a conversion on exactly the type it names - so a derived value would
        // be refused by the very rule that let its declaration through.
        [Fact]
        public void AByRefLikeParameterIsFedAValueOfADerivedType()
        {
            var parameter = Assert.Single(Assert.Single(
                BenchmarkConverter.TypeToBenchmarks(typeof(DerivedConvertedToByRefLike)).BenchmarksCases).Parameters.Items);

            Assert.IsType<DerivedConvertible>(parameter.Value);

            // The generated code holds the value as the type the declaration names, not the one it happens to be.
            Assert.Equal(typeof(Convertible), parameter.ParameterValue.SourceType);
        }

        public class DerivedConvertedToByRefLike
        {
            public static IEnumerable<Convertible> Values() { yield return new DerivedConvertible(); }
            [Benchmark][ArgumentsSource(nameof(Values))] public int Run(ReadOnlySpan<byte> a) => a.Length;
        }

        public class Convertible
        {
            public byte[] Bytes = [1, 2, 3];

            public static implicit operator ReadOnlySpan<byte>(Convertible convertible) => convertible.Bytes;
        }

        public class DerivedConvertible : Convertible { }

        // A typed array is not an object[], so none of these reach the branch that reads an element as an
        // argument list, whatever conversion the parameter goes on to apply to the array it is handed.
        public static class WholeArrayByConversion
        {
            public class ToReadOnlySpan
            {
                public static IEnumerable<byte[]> Values() { yield return [1, 2, 3]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(ReadOnlySpan<byte> a) => a.Length;
            }

            public class ToReadOnlyMemory
            {
                public static IEnumerable<byte[]> Values() { yield return [1, 2, 3]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(ReadOnlyMemory<byte> a) => a.Length;
            }

            public class ToMemory
            {
                public static IEnumerable<byte[]> Values() { yield return [1, 2, 3]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(Memory<byte> a) => a.Length;
            }

#if !NETFRAMEWORK
            public class ToArraySegment
            {
                public static IEnumerable<byte[]> Values() { yield return [1, 2, 3]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(ArraySegment<byte> a) => a.Count;
            }
#endif

            public class ToRefArray
            {
                public static IEnumerable<byte[]> Values() { yield return [1, 2, 3]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(ref byte[] a) => a.Length;
            }
        }

        // The index follows the declared element type and nothing else, which is what lets the generated code and
        // the in-process toolchains take the same argument out of a row: the generated code binds its extraction
        // against that type, and SmartParamBuilder takes the row apart the same way. Whether the source is
        // *written* as the interface or merely implements it does not enter into it, which is what the
        // List<object[]> and ArrayRows rows are here to hold - a decision read from the value instead of the
        // declaration would separate them.
        [Theory]
        // An object[] element feeding several parameters is the argument list, indexed.
        [InlineData(typeof(ArgumentListSource.Declared), 0)]
        [InlineData(typeof(ArgumentListSource.AsyncDeclared), 0)]
        // Only implementing the interface reads the same, because the element type is the same.
        [InlineData(typeof(ArgumentListSource.Implemented), 0)]
        [InlineData(typeof(ArgumentListSource.AsyncImplemented), 0)]
        // One parameter is fed the value itself, so nothing is indexed - here the parameter is declared object[],
        // which is what the element names, so the whole array is the argument however many items it holds.
        [InlineData(typeof(ArgumentListSource.DeclaredUnwrapped), null)]
        [InlineData(typeof(ArgumentListSource.DeclaredTooManyForOne), null)]
        [InlineData(typeof(ArgumentListSource.WholeArray), null)]
        [InlineData(typeof(ArgumentListSource.AsyncWholeArray), null)]
        public void TheIndexFollowsTheDeclaredElementType(Type benchmarkType, int? expected)
        {
            var parameter = Assert.Single(BenchmarkConverter.TypeToBenchmarks(benchmarkType).BenchmarksCases).Parameters.Items.First();

            Assert.Equal(expected, Assert.IsType<ParameterValue.FromSource>(parameter.ParameterValue).ElementIndex);
        }

        // An object[] element cannot feed a single parameter that is not itself an object[]: the declaration cannot
        // tell a one-element argument list apart from a value that happens to be an array, so it names neither.
        [Fact]
        public void AnObjectArrayIsRefusedForASingleParameterOfAnotherType()
        {
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(ArgumentListSource.DeclaredUnrecognised)).DeclarationErrors);

            Assert.Contains("is declared to yield Object[] for a IBox argument", error.Message);
            Assert.Contains("never a one-element array wrapping it", error.Message);
        }

        // The erased-enum display branch was reached only by attribute constants before this work, so the value
        // was always the enum's underlying type. A source can yield anything, and Enum.ToObject throws on anything
        // else - out of ToDisplayText, which runs while logging rather than while validating.
        // The arguments of one row come out of a single read, which is what lets a toolchain emit the read once
        // and index into it. Two reads that merely compare equal would not do: the renderer groups by identity.
        [Fact]
        public void ArgumentsOfOneRowShareOneRead()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(TwoArgumentsFromOneSource)).BenchmarksCases;

            foreach (var benchmark in benchmarks)
            {
                var reads = benchmark.Parameters.Items
                    .Select(parameter => Assert.IsType<ParameterValue.FromSource>(parameter.ParameterValue).Read)
                    .ToArray();

                Assert.Equal(2, reads.Length);
                Assert.Same(reads[0], reads[1]);
            }

            // ... and a different row is a different read.
            Assert.NotSame(
                Assert.IsType<ParameterValue.FromSource>(benchmarks.First().Parameters.Items.First().ParameterValue).Read,
                Assert.IsType<ParameterValue.FromSource>(benchmarks.Last().Parameters.Items.First().ParameterValue).Read);
        }

        public class TwoArgumentsFromOneSource
        {
            public class Box { public int Value { get; set; } }

            public static IEnumerable<object[]> Rows()
            {
                yield return [new Box { Value = 1 }, new Box { Value = 10 }];
                yield return [new Box { Value = 2 }, new Box { Value = 20 }];
            }

            [Benchmark][ArgumentsSource(nameof(Rows))] public int Run(Box a, Box b) => a.Value + b.Value;
        }

        [Fact]
        public void AnEnumParameterFedAValueThatIsNotItsUnderlyingTypeStillRenders()
        {
            var benchmark = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(MismatchedEnumSource)).BenchmarksCases);

            Assert.Contains("not an enum", benchmark.DisplayInfo);
        }

        // The reason the branch exists: F# erases an enum to its underlying type, so the declared type names it.
        [Fact]
        public void AnEnumParameterFedItsUnderlyingTypeRendersTheEnumName()
        {
            var benchmark = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(ErasedEnumSource)).BenchmarksCases);

            Assert.Contains("Green", benchmark.DisplayInfo);
        }

        public enum Colour { Red = 1, Green = 2 }

        public class MismatchedEnumSource
        {
            [ParamsSource(nameof(Values))]
            public Colour Value { get; set; }

            public static IEnumerable<object> Values() { yield return "not an enum"; }

            [Benchmark] public int Run() => (int) Value;
        }

        public class ErasedEnumSource
        {
            [ParamsSource(nameof(Values))]
            public Colour Value { get; set; }

            public static IEnumerable<object> Values() { yield return 2; }

            [Benchmark] public int Run() => (int) Value;
        }

        public static class ArgumentListSource
        {
            public class Declared
            {
                public static IEnumerable<object[]> Values() { yield return [new Box(), new Box()]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(Box a, Box b) => 0;
            }

            public class DeclaredUnwrapped
            {
                public static IEnumerable<object[]> Values() { yield return [new object[] { 1, 2 }]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(object[] a) => a.Length;
            }

            // The refused shape: object[] declared for a parameter that is not one. Whether the row happens to
            // hold exactly one item of the parameter's type is a property of the value, which the rule never reads.
            public class DeclaredUnrecognised
            {
                public static IEnumerable<object[]> Values() { yield return [new BoxImpl()]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(IBox a) => 0;
            }

            public class DeclaredTooManyForOne
            {
                public static IEnumerable<object[]> Values() { yield return [1, 2, 3]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(object[] a) => a.Length;
            }

            public class Implemented
            {
                public static List<object[]> Values() => [[new Box(), new Box()]];
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(Box a, Box b) => 0;
            }

            public class WholeArray
            {
                public static List<object[]> Values() => [[1, 2, 3]];
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(object[] a) => a.Length;
            }

            public class AsyncDeclared
            {
                public static async IAsyncEnumerable<object[]> Values() { await Task.Yield(); yield return [new Box(), new Box()]; }
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(Box a, Box b) => 0;
            }

            public class AsyncImplemented
            {
                public static ArrayRows Values() => new([new Box(), new Box()]);
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(Box a, Box b) => 0;
            }

            public class AsyncWholeArray
            {
                public static ArrayRows Values() => new([1, 2, 3]);
                [Benchmark][ArgumentsSource(nameof(Values))] public int Run(object[] a) => a.Length;
            }

            // Implements the interface without being written as it - the async counterpart of List<object[]>.
            public sealed class ArrayRows(object[] row) : IAsyncEnumerable<object[]>
            {
                public async IAsyncEnumerator<object[]> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                {
                    await Task.Yield();
                    yield return row;
                }
            }

            public class Box { }

            public interface IBox { }

            public class BoxImpl : IBox { }
        }

        [GenericTypeArguments(typeof(int))]
        public class MatchedGenericArgumentSource<T>
        {
            public static IEnumerable<T> Values() { yield return (T)(object)7; }

            [Benchmark]
            [ArgumentsSource(nameof(Values))]
            public T Run(T a) => a;
        }

        [GenericTypeArguments(typeof(byte[]))]
        public class SpanFromGenericArgumentSource<T>
        {
            public static IEnumerable<T> Values() { yield return (T)(object)new byte[] { 1, 2, 3 }; }

            [Benchmark]
            [ArgumentsSource(nameof(Values))]
            public int Run(ReadOnlySpan<byte> bytes) => bytes.Length;
        }

        [Fact]
        public void AnInheritedGenericSourceFeedingItsOwnTypeParameterIsAccepted()
        {
            // The same reading has to keep this one working: the base's T and the benchmark's T are the same
            // parameter once the base is written as the derived type names it.
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(InheritedMatchedSource<int>)).BenchmarksCases;

            Assert.Single(Assert.Single(benchmarks).Parameters.Items);
        }

        public class GenericSourceBase<T>
        {
            public static IEnumerable<T> Values() { yield return default!; }
        }

        public class InheritedMatchedSource<T> : GenericSourceBase<T>
        {
            [Benchmark][ArgumentsSource(nameof(Values))] public T Run(T a) => a;
        }

        [Fact]
        public void AGenericSourceMethodIsRejectedWithItsOwnMessage()
        {
            // Reflection would otherwise fail with "Late bound operations cannot be performed on types or methods
            // for which ContainsGenericParameters is true", which names nothing the user wrote.
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(GenericSourceMethod)).DeclarationErrors);

            Assert.Contains("is generic", error.Message);
            Assert.Contains(nameof(GenericSourceMethod.Values), error.Message);
        }

#pragma warning disable BDN1310
        public class GenericSourceMethod
        {
            public static IEnumerable<TItem> Values<TItem>() { yield return default!; }

            [Benchmark][ArgumentsSource(nameof(Values))] public int Run(int a) => a;
        }
#pragma warning restore BDN1310

        [Fact]
        public void APropertyIsPreferredOverAGenericMethodOfTheSameName()
        {
            // The generic method cannot be invoked, but it does not speak for the name: the property serves it.
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(GenericMethodHidingAProperty)).BenchmarksCases;

            Assert.Equal([7], benchmarks.Select(benchmark => benchmark.Parameters.Items.Single().Value));
        }

        public class PropertySourceBase
        {
            public static IEnumerable<int> Values => [7];
        }

        public class GenericMethodHidingAProperty : PropertySourceBase
        {
            public static new IEnumerable<TItem> Values<TItem>() { yield return default!; }

            [Benchmark][ArgumentsSource(nameof(Values))] public int Run(int a) => a;
        }

        [Fact]
        public void ASourceNamedThroughItsOwnBaseTypeIsReadAsWritten()
        {
            // typeof(Base<int>) fixes the source's type arguments, so it is not in the benchmark's generic context
            // even though the benchmark derives from that same base. Judging it there would compare int against U.
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(NamedThroughOwnBase<int, int>)).BenchmarksCases;

            Assert.Single(Assert.Single(benchmarks).Parameters.Items);
        }

        public class NamedThroughOwnBase<T, U> : GenericSourceBase<T>
        {
            [Benchmark]
            [ArgumentsSource(typeof(GenericSourceBase<int>), nameof(GenericSourceBase<int>.Values))]
            public U Run(U a) => a;
        }

        [Fact]
        public void ANonGenericOverloadIsPreferredOverAGenericOne()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(GenericAndNonGenericSourceOverloads)).BenchmarksCases;

            Assert.Equal([7], benchmarks.Select(benchmark => benchmark.Parameters.Items.Single().Value));
        }

        public class GenericAndNonGenericSourceOverloads
        {
            public static IEnumerable<TItem> Values<TItem>() { yield return default!; }

            public static IEnumerable<int> Values() { yield return 7; }

            [Benchmark][ArgumentsSource(nameof(Values))] public int Run(int a) => a;
        }

        [Theory]
        // A ref/in/out parameter reaches reflection as a byref type - `ref T` is `T&` - which nothing is castable to.
        // The modifier says how the argument travels, not what it has to be, so the comparison looks past it.
        [InlineData(typeof(RefParameterFromT<int, int>))]
        [InlineData(typeof(InParameterFromT<int, int>))]
        public void ARefModifierDoesNotChangeWhatTheParameterTakes(Type benchmarkType)
        {
            var benchmark = Assert.Single(BenchmarkConverter.TypeToBenchmarks(benchmarkType).BenchmarksCases);

            // The source yields default(T); the point is that the by-ref parameter produced a parameter at all.
            Assert.Equal(0, Assert.Single(benchmark.Parameters.Items).Value);
        }

        public class RefParameterFromT<T, U>
        {
            public static IEnumerable<T> Values() { yield return default!; }
            [Benchmark][ArgumentsSource(nameof(Values))] public T Run(ref T a) => a;
        }

        public class InParameterFromT<T, U>
        {
            public static IEnumerable<T> Values() { yield return default!; }
            [Benchmark][ArgumentsSource(nameof(Values))] public T Run(in T a) => a;
        }

        [Fact]
        public void AsyncDeclaredSourceIsReadAsynchronouslyEvenWhenTheValueIsAlsoEnumerable()
        {
            // Discovery must bind what the generated code binds - the declared IAsyncEnumerable<T> - rather than the
            // non-generic IEnumerable the returned object happens to also implement.
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(DualShapedSourceParams)).BenchmarksCases;

            var values = benchmarks
                .Select(b => b.Parameters.Items.Single(p => p.Name == nameof(DualShapedSourceParams.Value)).Value)
                .ToArray();

            Assert.Equal(new object[] { "async" }, values);
        }

        public class DualShapedSourceParams
        {
            public static IAsyncEnumerable<object> Values() => new DualShaped();

            [ParamsSource(nameof(Values))]
            public string Value { get; set; } = null!;

            [Benchmark]
            public string Run() => Value;

            // Async-first collection that also exposes a synchronous view, as a hand-written one often does.
            private sealed class DualShaped : IAsyncEnumerable<object>, System.Collections.IEnumerable
            {
                public async IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                {
                    await Task.Yield();
                    yield return "async";
                }

                public System.Collections.IEnumerator GetEnumerator()
                {
                    yield return "sync";
                }
            }
        }

        [Fact]
        public void AsyncEnumerablePatternParamsSourceIsRejected()
        {
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerablePatternParams)).DeclarationErrors);

            Assert.Contains(nameof(AsyncEnumerablePatternParams.Values), error.Message);
            Assert.Contains("does not implement IEnumerable or IAsyncEnumerable", error.Message);
        }

#pragma warning disable BDN1306
        public class AsyncEnumerablePatternParams
        {
            // A custom await-foreach shape that does NOT implement IAsyncEnumerable<T>.
            public sealed class PatternEnumerable
            {
                public PatternEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default) => new();
            }

            public sealed class PatternEnumerator
            {
                private int index = -1;
                private readonly int[] items = [10, 20];
                public int Current => items[index];
                public async ValueTask<bool> MoveNextAsync()
                {
                    await Task.Yield();
                    return ++index < items.Length;
                }
            }

            public static PatternEnumerable Values() => new();

            [ParamsSource(nameof(Values))]
            public int Value { get; set; }

            [Benchmark]
            public int Run() => Value;
        }
#pragma warning restore BDN1306

        [Fact]
        public void ARowThatIsNotTheDeclaredTupleIsReported()
        {
            var error = Assert.Single(BenchmarkConverter.TypeToBenchmarks(typeof(RowIsNotTheDeclaredTuple)).DeclarationErrors);

            Assert.Contains("expects an argument list from [ArgumentsSource(Values)], but Int32 was provided", error.Message);
        }

        public class RowIsNotTheDeclaredTuple
        {
            public static IEnumerable<(int, string)> Values() => new Disagrees();

            [Benchmark]
            [ArgumentsSource(nameof(Values))]
            public void Run(int a, string b) { }

            private class Disagrees : IEnumerable<(int, string)>
            {
                public IEnumerator<(int, string)> GetEnumerator() => Enumerable.Empty<(int, string)>().GetEnumerator();

                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new object[] { 42 }.GetEnumerator();
            }
        }
    }
}