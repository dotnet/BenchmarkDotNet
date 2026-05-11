---
uid: docs.async-enumerable
name: IAsyncEnumerable Benchmarks
---

# IAsyncEnumerable Benchmarks

Benchmark methods can return `IAsyncEnumerable<T>` (or any type that satisfies the C# `await foreach` pattern). The framework drives the iteration to completion on every benchmark invocation, the same way a `await foreach` consumer would, and the elapsed time covers everything from the factory call through the final `MoveNextAsync` that observes end-of-stream.

```csharp
public class AsyncEnumerableBenchmarks
{
    [Benchmark]
    public async IAsyncEnumerable<int> Produce()
    {
        for (int i = 0; i < 1000; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}
```

## What gets measured

For each invocation, the framework:

1. Calls the workload method to obtain the enumerable (compiler-generated state machines do no work here — the body runs lazily).
2. Calls `GetAsyncEnumerator()` and drives `MoveNextAsync()` / `Current` until the stream ends.

Disposal (`DisposeAsync`) is invoked when the enumerator implements `IAsyncDisposable` or exposes a public `DisposeAsync()` matching the C# pattern, mirroring `await foreach` semantics.

## Custom enumerable types

`IsAsyncEnumerable` detection mirrors the C# `await foreach` resolution rules: it accepts any type that **is** `IAsyncEnumerable<T>`, has a public `GetAsyncEnumerator` method matching the pattern (with all parameters optional, including `CancellationToken`), or implements the interface explicitly. The element type comes from `Current` so it tracks what the compiler binds to — even when a type defines a public pattern method whose `Current` differs from the explicitly-implemented `IAsyncEnumerable<U>.GetAsyncEnumerator`'s `U`. This means hand-written enumerables, ref-struct enumerables, and `ConfiguredCancelableAsyncEnumerable<T>` all work without special configuration.

## Cancellation

The framework does not implicitly call `.WithCancellation(token)` on the returned enumerable. Two reasons:

1. It would make the no-cancellation path unmeasurable.
2. For types that implement `IAsyncEnumerable<T>` *and* define a public pattern `GetAsyncEnumerator`, `WithCancellation` flips dispatch from the pattern to the interface — silently changing what gets measured.

You have two opt-in routes:

**Class-level field** — works for any benchmark, no return-type changes needed. The iterator can read the token directly:

```csharp
public class CancellableProducer
{
    [BenchmarkCancellation]
    public CancellationToken CancellationToken;

    [Benchmark]
    public async IAsyncEnumerable<int> Produce()
    {
        for (int i = 0; i < 1000; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return i;
        }
    }
}
```

**`[EnumeratorCancellation]` propagation** — return `ConfiguredCancelableAsyncEnumerable<T>` directly from the benchmark to opt the iterator into the C#-idiomatic pattern. The framework's `await foreach` over the returned configured enumerable will propagate the token to a parameter marked `[EnumeratorCancellation]` inside your iterator:

```csharp
public class EnumeratorCancellationProducer
{
    [BenchmarkCancellation]
    public CancellationToken CancellationToken;

    [Benchmark]
    public ConfiguredCancelableAsyncEnumerable<int> Produce()
        => Inner().WithCancellation(CancellationToken);

    private static async IAsyncEnumerable<int> Inner([EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = 0; i < 1000; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return i;
        }
    }
}
```

## Setup and cleanup

`[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, and `[IterationCleanup]` methods may **not** return `IAsyncEnumerable<T>`. The framework only awaits awaitables in those positions; it would call the iterator factory and discard the enumerable without ever running the body. A mandatory validator rejects this at startup with a clear error — return `void`, `Task`, `ValueTask`, or any awaitable type instead.

## DisassemblyDiagnoser

`[DisassemblyDiagnoser]` walks the benchmark's call graph from a synthetic entry method that just invokes the workload and discards the result. For `IAsyncEnumerable<T>` benchmarks that means the iteration body — `GetAsyncEnumerator`, `MoveNextAsync`, `Current` — is never reached from the entry, regardless of whether the return type is the interface or a custom struct/sealed type. Compiler-generated async iterators only kick off their state machine on the first `MoveNextAsync`, so the disassembler sees nothing of substance from the workload call alone.

To inspect the iterator body, use the diagnoser's `filters` parameter:

```csharp
[DisassemblyDiagnoser(filters: ["*MoveNextAsync*"])]
public class MyBenchmarks { /* ... */ }
```
