# Cooperative Cancellation in Benchmarks

BenchmarkDotNet supports cooperative cancellation, allowing benchmarks to gracefully respond to cancellation requests such as Ctrl+C.

## CancellationToken Injection

For long-running individual benchmark iterations, you can make your benchmarks cooperatively cancellable by marking a property or field with the `[BenchmarkCancellation]` attribute:

[!code-csharp[IntroCancellationToken.cs](../../../samples/BenchmarkDotNet.Samples/IntroCancellationToken.cs)]

You can also use a field instead of a property:

```csharp
public class MyBenchmarks
{
    [BenchmarkCancellation]
    public CancellationToken CancellationToken;

    [Benchmark]
    public async Task MyBenchmark()
    {
        await Task.Delay(100, CancellationToken);
    }
}
```

## How It Works

1. **Attribute Detection**: BenchmarkDotNet automatically detects properties or fields marked with `[BenchmarkCancellation]`
2. **Automatic Injection**: Before running benchmarks, the framework injects the current cancellation token
3. **Cooperative Checking**: Your benchmark code passes the token to async methods or calls `ThrowIfCancellationRequested()`

## Best Practices

1. **Pass to async methods**: Pass the token to async framework methods that already support cancellation
2. **Don't swallow cancellation**: Let `OperationCanceledException` propagate
3. **Check periodically in tight loops**: For CPU-bound loops, check every N iterations to balance responsiveness and overhead

## Requirements

Properties and fields marked with `[BenchmarkCancellation]` must be:
- Of type `System.Threading.CancellationToken`
- Public
- For properties: must have a public setter (init-only setters are supported)

Both static and instance members are supported.
