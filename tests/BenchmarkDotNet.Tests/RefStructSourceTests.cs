using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections;

// The framework's IEnumerable<T> only took an allows-ref-struct type parameter in .NET 10, and the constraint
// itself needs RuntimeFeature.ByRefLikeGenerics, which .NET Framework does not have. Every declaration in this
// file is one of those shapes; the rules they cover are not framework-specific.
#if NET10_0_OR_GREATER
namespace BenchmarkDotNet.Tests;

// Since .NET 10 the framework's IEnumerable<T> allows a ref struct type argument, so a source can be declared to
// yield one. Reading the values puts each into an object[], which a ref struct cannot enter, so the enumeration
// fails inside reflection saying nothing about the benchmark. Discovery names it, and BDN1311 reports it at build time.
public class RefStructSourceTests
{
    [Fact]
    public void DiscoveryReportsARefStructElement()
    {
        var exception = Assert.Throws<InvalidBenchmarkDeclarationException>(
            () => BenchmarkConverter.TypeToBenchmarks(typeof(YieldsRefStruct)));

        Assert.Contains("which is a ref struct", exception.Message);
        Assert.Contains("Span<Int32>", exception.Message);
    }

    public class YieldsRefStruct
    {
        public IEnumerable<Span<int>> Data() => new SpanSequence();

        [Benchmark]
#pragma warning disable BDN1311
        [ArgumentsSource(nameof(Data))]
#pragma warning restore BDN1311
        public int Run(Span<int> argument) => argument.Length;
    }

    // An async source reads its values into the same object[], so the shape is asked of both and ahead of either.
    [Fact]
    public void DiscoveryReportsARefStructElementFromAnAsyncSource()
    {
        var exception = Assert.Throws<InvalidBenchmarkDeclarationException>(
            () => BenchmarkConverter.TypeToBenchmarks(typeof(YieldsRefStructAsynchronously)));

        Assert.Contains("which is a ref struct", exception.Message);
        Assert.Contains("Span<Int32>", exception.Message);
    }

    public class YieldsRefStructAsynchronously
    {
        public IAsyncEnumerable<Span<int>> Data() => new AsyncSpanSequence();

        [Benchmark]
#pragma warning disable BDN1311
        [ArgumentsSource(nameof(Data))]
#pragma warning restore BDN1311
        public int Run(Span<int> argument) => argument.Length;
    }

    // The substitution, not the declaration, decides this: a source declared to yield a type parameter that merely
    // admits a ref struct is read like any other value type when closed to one that is not, and named here when it
    // is. SourceReturnTypeValidator therefore leaves the shape alone - judging the open declaration would report
    // only the substitutions that work.
    [Fact]
    public void DiscoveryReportsARefStructSubstitution()
    {
        var exception = Assert.Throws<InvalidBenchmarkDeclarationException>(
            () => BenchmarkConverter.TypeToBenchmarks(typeof(AdmitsARefStruct<Span<int>>)));

        Assert.Contains("which is a ref struct", exception.Message);
        Assert.Contains("Span<Int32>", exception.Message);
    }

    [Fact]
    public void ANonRefStructSubstitutionIsAccepted()
        => Assert.NotEmpty(BenchmarkConverter.TypeToBenchmarks(typeof(AdmitsARefStruct<int>)).BenchmarksCases);

    public class AdmitsARefStruct<T> where T : allows ref struct
    {
        public static IEnumerable<T> Values() => new OneValue<T>();

        [Benchmark]
#pragma warning disable BDN1312
        [ArgumentsSource(nameof(Values))]
#pragma warning restore BDN1312
        public void Run(T value) { }
    }

    private sealed class OneValue<T> : IEnumerable<T>, IEnumerator<T> where T : allows ref struct
    {
        private int index = -1;

        public T Current => default!;
        object IEnumerator.Current => null!;

        public IEnumerator<T> GetEnumerator() => new OneValue<T>();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext() => ++index == 0;
        public void Reset() => index = -1;
        public void Dispose() { }
    }

    private sealed class AsyncSpanSequence : IAsyncEnumerable<Span<int>>, IAsyncEnumerator<Span<int>>
    {
        private int index;
        public Span<int> Current => new int[] { 1, 2, 3 };
        public IAsyncEnumerator<Span<int>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;
        public ValueTask<bool> MoveNextAsync() => new(index++ < 1);
        public ValueTask DisposeAsync() => default;
    }

    private sealed class SpanSequence : IEnumerable<Span<int>>, IEnumerator<Span<int>>
    {
        private int index;
        public Span<int> Current => new int[] { 1, 2, 3 };
        object IEnumerator.Current => throw new NotSupportedException();
        public IEnumerator<Span<int>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        public bool MoveNext() => index++ < 1;
        public void Reset() => index = 0;
        public void Dispose() { }
    }
}
#endif
