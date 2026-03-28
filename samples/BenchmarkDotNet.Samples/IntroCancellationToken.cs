using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples;

/// <summary>
/// Demonstrates cooperative cancellation using [BenchmarkCancellation] attribute.
/// When a benchmark class has a property or field marked with [BenchmarkCancellation], BenchmarkDotNet automatically
/// injects the cancellation token, allowing benchmarks to check for cancellation during execution.
/// This is useful for long-running async benchmarks that should respond to Ctrl+C or other cancellation signals.
/// </summary>
public class IntroCancellationToken
{
    [BenchmarkCancellation]
    public CancellationToken CancellationToken { get; set; }

    [Benchmark]
    public async Task AsyncBenchmark()
    {
        for (int i = 0; i < 100; i++)
        {
            await DoWorkAsync(CancellationToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        // Simulate some async work
        await Task.Delay(100, cancellationToken);
    }
}
