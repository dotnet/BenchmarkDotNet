using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Tests.Helpers;

public class DynamicAwaitHelperTests
{

    // The caller's cancellation token must reach the source's [EnumeratorCancellation] for both element kinds:
    // reference types go through the covariance cast, value types through the reflection loop (+ GetAsyncEnumeratorArgs).
    // Only the source path takes a token at all - EnumerateBenchmarkAsync deliberately has none, because real
    // execution never forces the ambient token onto a benchmark's own sequence.
    [Theory]
    [InlineData(typeof(IAsyncEnumerable<int>))]    // value-type element -> reflection loop
    [InlineData(typeof(IAsyncEnumerable<string>))] // reference-type element -> covariance cast
    public async Task EnumerateSourceAsync_ForwardsCancellationTokenToSource(Type asyncEnumerableType)
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        object source = asyncEnumerableType == typeof(IAsyncEnumerable<int>)
            ? ValueTypeSource()
            : ReferenceTypeSource();

        var enumerable = DynamicAwaitHelper.EnumerateSourceAsync(source, asyncEnumerableType.GetGenericArguments()[0]);

        var exception = await Record.ExceptionAsync(async () =>
        {
            await foreach (var _ in enumerable.WithCancellation(cts.Token))
            {
            }
        });

        // Both paths surface the OperationCanceledException itself. The covariance path never reflects, and the
        // reflection loop routes every reflective call through DynamicAwaitHelper.Unwrapped - GetResult included,
        // which is where an awaited MoveNextAsync throws - so the TargetInvocationException is already gone.
        // Asserted directly rather than through an InnerException fallback, which could never fire and would have
        // let a lost unwrap pass.
        Assert.NotNull(exception);
        var operationCanceled = Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.Equal(cts.Token, operationCanceled.CancellationToken);
    }

    // A type can implement IAsyncEnumerable<T> and still bind await-foreach to its own pattern method. Draining it
    // must follow the pattern - what the benchmark itself enumerates - not the interface.
    [Fact]
    public async Task EnumerateAsync_PrefersThePatternOverTheImplementedInterface()
    {
        // The declared type is the interface: that is where the pattern and IAsyncEnumerable<T> both apply.
        Assert.True(typeof(IPatternAndInterfaceSource).IsAsyncEnumerable(out var info));

        var items = new List<object?>();
        await foreach (var item in DynamicAwaitHelper.EnumerateBenchmarkAsync(new PatternAndInterfaceSource(), info!))
        {
            items.Add(item);
        }

        Assert.Equal(new object?[] { "pattern" }, items);
    }

    // An optional parameter can lack a declared default ([Optional] with no [DefaultParameterValue]). Guards the
    // argument defaulting: Type.Missing would be rejected by Invoke, unlike the null this passes.
    [Fact]
    public async Task EnumerateAsync_HandlesOptionalParametersWithoutADeclaredDefault()
    {
        Assert.True(typeof(OptionalWithoutDefaultSource).IsAsyncEnumerable(out var info));

        var items = new List<object?>();
        await foreach (var item in DynamicAwaitHelper.EnumerateBenchmarkAsync(new OptionalWithoutDefaultSource(), info!))
        {
            items.Add(item);
        }

        Assert.Equal(new object?[] { 0 }, items);
    }

    private sealed class OptionalWithoutDefaultSource
    {
        // Value-type element, so enumeration goes through the reflection loop and its argument defaulting.
        public OptionalEnumerator GetAsyncEnumerator([System.Runtime.InteropServices.Optional] CancellationToken cancellationToken) => new();
    }

    private sealed class OptionalEnumerator
    {
        private bool moved;
        public int Current => 0;

        public ValueTask<bool> MoveNextAsync([System.Runtime.InteropServices.Optional] bool unused)
        {
            bool hasMore = !moved;
            moved = true;
            return new ValueTask<bool>(hasMore);
        }
    }

    private interface IPatternAndInterfaceSource : IAsyncEnumerable<string>
    {
        // await-foreach binds to this in preference to the inherited IAsyncEnumerable<string> member.
        new PatternEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default);
    }

    private sealed class PatternAndInterfaceSource : IPatternAndInterfaceSource
    {
        public PatternEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default) => new();

        IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => new InterfaceEnumerator();
    }

    private sealed class PatternEnumerator
    {
        private bool moved;
        public string Current => "pattern";

        public ValueTask<bool> MoveNextAsync()
        {
            bool hasMore = !moved;
            moved = true;
            return new ValueTask<bool>(hasMore);
        }
    }

    private sealed class InterfaceEnumerator : IAsyncEnumerator<string>
    {
        private bool moved;
        public string Current => "interface";

        public ValueTask<bool> MoveNextAsync()
        {
            bool hasMore = !moved;
            moved = true;
            return new ValueTask<bool>(hasMore);
        }

        public ValueTask DisposeAsync() => default;
    }

    // The element-type overload binds IAsyncEnumerable<T>, so it must reach the implementation however the source
    // declares it - and must not be diverted by an await-foreach pattern method the source also happens to declare.
    [Theory]
    [InlineData(typeof(ImplicitValueSource), 1)]
    [InlineData(typeof(ExplicitValueSource), 2)]
    [InlineData(typeof(PatternAndInterfaceValueSource), 3)]
    [InlineData(typeof(BrokenPatternAndInterfaceValueSource), 4)]
    public async Task EnumerateAsync_BindsTheInterface_ForEveryImplementationShape(Type sourceType, int expected)
    {
        object source = Activator.CreateInstance(sourceType)!;

        // Discovery's gate: the interface check is what routes here.
        Assert.True(sourceType.IsIAsyncEnumerable(out var elementType));
        Assert.Equal(typeof(int), elementType);

        List<object?> items = [];
        await foreach (var item in DynamicAwaitHelper.EnumerateSourceAsync(source, elementType!))
        {
            items.Add(item);
        }

        Assert.Equal([expected], items);
    }

    private sealed class ImplicitValueSource : IAsyncEnumerable<int>
    {
        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => Yield(1).GetAsyncEnumerator(cancellationToken);
    }

    private sealed class ExplicitValueSource : IAsyncEnumerable<int>
    {
        IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => Yield(2).GetAsyncEnumerator(cancellationToken);
    }

    // Declares a conforming await-foreach pattern method over a different element type, explicitly implementing the
    // interface. Binding the pattern would enumerate strings; the interface is the source contract, so it wins.
    private sealed class PatternAndInterfaceValueSource : IAsyncEnumerable<int>
    {
        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => Yield("pattern").GetAsyncEnumerator(cancellationToken);

        IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => Yield(3).GetAsyncEnumerator(cancellationToken);
    }

    // Declares a pattern method whose return type is not an enumerator at all. IsAsyncEnumerable commits to the
    // pattern and rejects such a type outright, so resolving through it would throw on a usable source.
    private sealed class BrokenPatternAndInterfaceValueSource : IAsyncEnumerable<int>
    {
        public int GetAsyncEnumerator(CancellationToken cancellationToken = default) => 0;

        IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => Yield(4).GetAsyncEnumerator(cancellationToken);
    }

    private static async IAsyncEnumerable<T> Yield<T>(T value)
    {
        await Task.Yield();
        yield return value;
    }

    private static async IAsyncEnumerable<int> ValueTypeSource([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return 1;
    }

    private static async IAsyncEnumerable<string> ReferenceTypeSource([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return "value";
    }

    // ICriticalNotifyCompletion is optional in the awaiter pattern. DynamicAwaiter declares it, so a state machine
    // awaiting one always calls UnsafeOnCompleted - which must not ask the user awaiter for an interface map it
    // cannot supply.
    [Fact]
    public async Task EnumerateAsync_AwaiterImplementingOnlyINotifyCompletion()
    {
        var values = new List<object?>();
        await foreach (var value in DynamicAwaitHelper.EnumerateBenchmarkAsync(new PolitelyAwaitableSource(), Info<PolitelyAwaitableSource>()))
            values.Add(value);

        Assert.Equal([1, 2], values);
    }

    // The awaiter pattern binds on the awaiter's *declared* type, which may be an interface. Nothing can be asked
    // for an interface map about one, so the map lookup must not be reached for it.
    [Fact]
    public async Task EnumerateAsync_AwaiterDeclaredAsAnInterface()
    {
        var values = new List<object?>();
        await foreach (var value in DynamicAwaitHelper.EnumerateBenchmarkAsync(new InterfaceAwaitedSource(), Info<InterfaceAwaitedSource>()))
            values.Add(value);

        Assert.Equal([1, 2], values);
    }

    private static AsyncEnumerableInfo Info<T>()
    {
        Assert.True(typeof(T).IsAsyncEnumerable(out var info));
        return info!;
    }

    private sealed class PolitelyAwaitableSource
    {
        public PoliteEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default) => new();
    }

    private sealed class PoliteEnumerator
    {
        private int index;
        public int Current => index;
        public PoliteAwaitable MoveNextAsync() => new(++index <= 2);
    }

    private readonly struct PoliteAwaitable(bool result)
    {
        public PoliteAwaiter GetAwaiter() => new(result);
    }

    // Deliberately NOT ICriticalNotifyCompletion - the awaiter pattern does not require it.
    private readonly struct PoliteAwaiter(bool result) : INotifyCompletion
    {
        public bool IsCompleted => false;
        public bool GetResult() => result;
        public void OnCompleted(Action continuation) => ThreadPool.QueueUserWorkItem(_ => continuation());
    }

    private sealed class InterfaceAwaitedSource
    {
        public InterfaceAwaitedEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default) => new();
    }

    private sealed class InterfaceAwaitedEnumerator
    {
        private int index;
        public int Current => index;
        public InterfaceAwaitable MoveNextAsync() => new(++index <= 2);
    }

    private readonly struct InterfaceAwaitable(bool result)
    {
        // Declared as the interface: legal in the awaiter pattern, and it makes AwaiterType an interface.
        public IBoolAwaiter GetAwaiter() => new BoolAwaiter(result);
    }

    private interface IBoolAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        bool GetResult();
    }

    // Implements INotifyCompletion explicitly, so this also covers the reason the interface map was consulted:
    // dispatching through the interface method has to reach an explicit implementation.
    private sealed class BoolAwaiter(bool result) : IBoolAwaiter
    {
        public bool IsCompleted => false;
        public bool GetResult() => result;
        void INotifyCompletion.OnCompleted(Action continuation) => ThreadPool.QueueUserWorkItem(_ => continuation());
    }
}
