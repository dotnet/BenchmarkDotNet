using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

#pragma warning disable BDN1306
#pragma warning disable BDN1308
#pragma warning disable BDN1311
#pragma warning disable BDN1312
#pragma warning disable BDN1504

namespace BenchmarkDotNet.Tests.Validators;

public class SourceReturnTypeValidatorTests
{
    private static async ValueTask<string[]> Validate<T>()
    {
        var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(T));
        // Without a case to validate, every "is not reported" assertion below would hold for the wrong reason.
        Assert.NotEmpty(benchmarks.BenchmarksCases);

        var errors = await SourceReturnTypeValidator.FailOnError.ValidateAsync(benchmarks).ToArrayAsync();
        return errors.Select(error => error.Message).ToArray();
    }

    [Fact]
    public async Task GenericEnumerableSourceIsNotReported()
    {
        Assert.Empty(await Validate<WithGenericEnumerableSource>());
    }

    [Fact]
    public async Task NonGenericEnumerableSourceIsReported()
    {
        var messages = await Validate<WithNonGenericEnumerableSource>();

        Assert.Contains(messages, message => message.Contains("neither IEnumerable<T> nor IAsyncEnumerable<T>"));
    }

    [Fact]
    public async Task SeveralEnumerableInstantiationsAreReported()
    {
        var messages = await Validate<WithTwoElementTypesSource>();

        Assert.Contains(messages, message => message.Contains("more than one enumerable shape"));
    }

    [Fact]
    public async Task SeveralAsyncEnumerableInstantiationsAreReported()
    {
        var messages = await Validate<WithTwoAsyncElementTypesSource>();

        Assert.Contains(messages, message => message.Contains("more than one enumerable shape"));
    }

    // An instance source on an unrelated type is left alone, as master leaves it. The generated runnable reaches
    // an instance source through `base`, which resolves nothing there, so a non-constant value does not compile
    // out-of-process - but a source whose values are all compile-time constants is rendered inline and runs, on
    // master and here. Reporting it would reject a shape master accepts, and that break is not this PR's to make.
    [Fact]
    public async Task InstanceSourceInAnotherTypeIsNotReported()
    {
        Assert.Empty(await Validate<WithInstanceSourceInAnotherType>());
    }

    [Fact]
    public async Task StaticSourceInAnotherTypeIsNotReported()
    {
        Assert.Empty(await Validate<WithStaticSourceInAnotherType>());
    }

    // Discovery keeps only the most derived declaration, so the hidden one's source is never read. Scanning
    // members separately reported it anyway - and this validator is mandatory, so the benchmark could not run.
    // A field, because reflection collapses a hidden property and hands back both declarations of a field.
    [Fact]
    public async Task ASourceOnAHiddenMemberIsNotReported()
    {
        Assert.Empty(await Validate<HidesABadParamsSource>());
    }

    public class BadParamsSourceOnABase
    {
        [ParamsSource(nameof(NotEnumerable))] public int Value;

        public static int NotEnumerable() => 0;
    }

    public class HidesABadParamsSource : BadParamsSourceOnABase
    {
        [ParamsSource(nameof(Enumerable))] public new int Value;

        public static IEnumerable<int> Enumerable() => [1];

        [Benchmark] public int Run() => Value;
    }

    public class SeparateSource
    {
        public IEnumerable<int> Instance() => [1, 2];

        public static IEnumerable<int> Static() => [1, 2];
    }

    public class WithInstanceSourceInAnotherType
    {
        [Benchmark]
        [ArgumentsSource(typeof(SeparateSource), nameof(SeparateSource.Instance))]
        public void Run(int value) { }
    }

    public class WithStaticSourceInAnotherType
    {
        [Benchmark]
        [ArgumentsSource(typeof(SeparateSource), nameof(SeparateSource.Static))]
        public void Run(int value) { }
    }

    // `allows ref struct` needs RuntimeFeature.ByRefLikeGenerics, which .NET Framework does not have, and the
    // framework's IEnumerable<T> only took the constraint in .NET 10. The rule itself is not framework-specific;
    // only a declaration that exercises it is.
#if NET10_0_OR_GREATER
    // The constraint admits a ref struct, but this substitution is not one, so the values read as any other value
    // type's do. A substitution that *is* by-ref-like never reaches a validator - discovery names it and throws
    // while reading the values, which RefStructSourceTests pins - so judging the declaration here would report
    // only the substitutions that work.
    [Fact]
    public async Task ArgumentsSourceYieldingAParameterAdmittingARefStructIsNotReported()
    {
        Assert.Empty(await Validate<AdmitsARefStruct<int>>());
    }

    [Fact]
    public async Task ParamsSourceYieldingAParameterAdmittingARefStructIsNotReported()
    {
        Assert.Empty(await Validate<AdmitsARefStructParam<int>>());
    }

    // The derived type fixes the argument, so the constraint stops deciding anything.
    [Fact]
    public async Task ASourceClosedByTheDerivedTypeIsNotReported()
    {
        Assert.Empty(await Validate<ClosesTheArgument>());
    }

    // One value, so a benchmark case exists for the validator to see. T is never actually a ref struct here -
    // reading one is what the rule forbids - so `default` is only ever boxed as the substitution allows.
    public class OneValue<T> : IEnumerable<T>, IEnumerator<T> where T : allows ref struct
    {
        private int index = -1;

        public T Current => default!;
        object System.Collections.IEnumerator.Current => null!;

        public IEnumerator<T> GetEnumerator() => new OneValue<T>();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext() => ++index == 0;
        public void Reset() => index = -1;
        public void Dispose() { }
    }

    public class AdmitsARefStruct<T> where T : allows ref struct
    {
        public static IEnumerable<T> Values() => new OneValue<T>();

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public void Run(T value) { }
    }

    public class AdmitsARefStructParam<T> where T : allows ref struct
    {
        public static IEnumerable<T> Values() => new OneValue<T>();

        [ParamsSource(nameof(Values))]
        public int Value { get; set; }

        [Benchmark] public void Run() { }
    }

    public class ValuesInAGenericBase<T> where T : allows ref struct
    {
        public static IEnumerable<T> Values() => new OneValue<T>();
    }

    public class ClosesTheArgument : ValuesInAGenericBase<int>
    {
        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public void Run(int value) { }
    }
#endif

    [Fact]
    public async Task NonGenericArgumentsSourceIsReported()
    {
        var messages = await Validate<WithNonGenericArgumentsSource>();

        Assert.Contains(messages, message => message.Contains("neither IEnumerable<T> nor IAsyncEnumerable<T>"));
    }

    public class WithGenericEnumerableSource
    {
        [ParamsSource(nameof(Values))]
        public int Value { get; set; }

        public static IEnumerable<int> Values() => [1, 2];

        [Benchmark] public void Run() { }
    }

    public class WithNonGenericEnumerableSource
    {
        [ParamsSource(nameof(Values))]
        public int Value { get; set; }

        public static System.Collections.IEnumerable Values() => new object[] { 1, 2 };

        [Benchmark] public void Run() { }
    }

    public class WithTwoElementTypesSource
    {
        [ParamsSource(nameof(Values))]
        public int Value { get; set; }

        public static TwoElementTypes Values() => new();

        [Benchmark] public void Run() { }
    }

    public class WithTwoAsyncElementTypesSource
    {
        [ParamsSource(nameof(Values))]
        public int Value { get; set; }

        public static TwoAsyncElementTypes Values() => new();

        [Benchmark] public void Run() { }
    }

    public class WithNonGenericArgumentsSource
    {
        public static System.Collections.IEnumerable Values() => new object[] { 1, 2 };

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public void Run(int value) { }
    }

    public class TwoElementTypes : IEnumerable<int>, IEnumerable<string>
    {
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => Ints().GetEnumerator();
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => Strings().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Ints().GetEnumerator();

        private static List<int> Ints() => [1, 2];
        private static List<string> Strings() => ["a"];
    }

    public class TwoAsyncElementTypes : IAsyncEnumerable<int>, IAsyncEnumerable<string>
    {
        IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => Ints().GetAsyncEnumerator(cancellationToken);

        IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => Strings().GetAsyncEnumerator(cancellationToken);

        private static async IAsyncEnumerable<int> Ints()
        {
            yield return 1;
            await Task.Yield();
            yield return 2;
        }

        private static async IAsyncEnumerable<string> Strings()
        {
            yield return "a";
            await Task.Yield();
        }
    }

    [Fact]
    public void ABaseTypeStaticParamsSourceIsBoundAndValidated()
    {
        // A base type's static member is a parameter like any other, so discovery reads it - and reaches this
        // source, which is not enumerable, before the validator gets a turn. Binding it at all is the point:
        // unbound, it would yield neither a parameter nor a diagnostic.
        var exception = Assert.Throws<InvalidBenchmarkDeclarationException>(
            () => BenchmarkConverter.TypeToBenchmarks(typeof(InheritsABadStaticParamsSource)));

        Assert.Contains(nameof(BadStaticParamsSourceOnABase.NotASource), exception.Message);
    }

    public class BadStaticParamsSourceOnABase
    {
        // BDN1306 is disabled for the whole file; a local restore here would re-enable it below, since restore
        // returns to the project default rather than to the enclosing directive.
        [ParamsSource(nameof(NotASource))]
        public static int InheritedParameter;

        public static int NotASource() => 0;
    }

    public class InheritsABadStaticParamsSource : BadStaticParamsSourceOnABase
    {
        [Benchmark]
        public int Run() => InheritedParameter;
    }
}
