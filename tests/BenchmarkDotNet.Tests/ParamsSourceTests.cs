using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

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
            var exception = Assert.Throws<InvalidBenchmarkDeclarationException>(
                () => BenchmarkConverter.TypeToBenchmarks(typeof(ClassWithWriteOnlyProperty)));

            Assert.Contains(nameof(ClassWithWriteOnlyProperty.WriteOnlyValues), exception.Message);
            Assert.Contains("no public, accessible method/property", exception.Message);
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
        [InlineData(typeof(WholeArrayByConversion.ToReadOnlyMemory))]
        [InlineData(typeof(WholeArrayByConversion.ToMemory))]
#if !NETFRAMEWORK
        [InlineData(typeof(WholeArrayByConversion.ToArraySegment))]
#endif
        [InlineData(typeof(WholeArrayByConversion.ToRefArray))]
        public void AnArrayAParameterIsDeclaredAsOrBuiltFromIsNotAnArgumentList(Type benchmarkType)
        {
            var parameter = Assert.Single(Assert.Single(BenchmarkConverter.TypeToBenchmarks(benchmarkType).BenchmarksCases).Parameters.Items);

            Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<byte[]>(parameter.Value));
        }

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

        // Every row is one cell of the matrix this rule was measured against, and the rows must keep disagreeing
        // along both axes: which interface the source is *written* as, and which branch of CreateForArguments
        // took the value. Two candidates were tried and rejected, and each survives a theory that varies only one
        // axis - reading the declared element type instead flips the two List<object[]> rows, and additionally
        // requiring the value to have come out of the array flips the two written-as-the-interface rows that fall
        // through. Keep at least one row on each side of both.
        [Theory]
        // Written as the interface, per-item branch: indexed, and every toolchain agrees.
        [InlineData(typeof(ArgumentListSource.Declared), 0)]
        // Written as the interface, unwrap branch - the element is the argument.
        [InlineData(typeof(ArgumentListSource.DeclaredUnwrapped), 0)]
        // Written as the interface but falling through, so the value is the whole array while the generated code
        // indexes it. Master's defect, and the reason the index cannot be read from the branch alone.
        [InlineData(typeof(ArgumentListSource.DeclaredUnrecognised), 0)]
        [InlineData(typeof(ArgumentListSource.DeclaredTooManyForOne), 0)]
        // Only implementing it, so it is not indexed - which costs the multi-argument case an index it needs.
        // Master's defect, and the reason the index cannot be read from the element type alone.
        [InlineData(typeof(ArgumentListSource.Implemented), null)]
        // Only implementing it, and falling through: the whole array is the value and nothing indexes it.
        [InlineData(typeof(ArgumentListSource.WholeArray), null)]
        // The async half follows the same rule, and has to: rewriting a source from IEnumerable<object[]> to
        // IAsyncEnumerable<object[]> may not change how its elements map onto arguments. There is no master
        // behaviour to reproduce on this side, so these three rows are what holds the two halves together.
        // Widening only the async side would not remove a defect, it would move one: AsyncImplemented would gain
        // the index it wants, and AsyncWholeArray would lose the agreement it has - and of the two, the whole-array
        // cell is the one that fails silently, with the generated code indexing what the in-process toolchains hand
        // over whole. See the note on SmartParamBuilder.Indexes.
        [InlineData(typeof(ArgumentListSource.AsyncDeclared), 0)]
        [InlineData(typeof(ArgumentListSource.AsyncImplemented), null)]
        [InlineData(typeof(ArgumentListSource.AsyncWholeArray), null)]
        public void TheIndexFollowsTheInterfaceTheSourceIsWrittenAs(Type benchmarkType, int? expected)
        {
            var parameter = Assert.Single(BenchmarkConverter.TypeToBenchmarks(benchmarkType).BenchmarksCases).Parameters.Items.First();

            Assert.Equal(expected, Assert.IsType<ParameterValue.FromSource>(parameter.ParameterValue).ElementIndex);
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

            // One element for one parameter, but its runtime type is not the parameter's, so the single-argument
            // branch declines it and the whole array is handed over.
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
            var exception = Assert.Throws<InvalidBenchmarkDeclarationException>(
                () => BenchmarkConverter.TypeToBenchmarks(typeof(GenericSourceMethod)));

            Assert.Contains("is generic", exception.Message);
            Assert.Contains(nameof(GenericSourceMethod.Values), exception.Message);
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
            var exception = Assert.Throws<InvalidBenchmarkDeclarationException>(
                () => BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerablePatternParams)));

            Assert.Contains(nameof(AsyncEnumerablePatternParams.Values), exception.Message);
            Assert.Contains("does not implement IEnumerable or IAsyncEnumerable", exception.Message);
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
    }
}