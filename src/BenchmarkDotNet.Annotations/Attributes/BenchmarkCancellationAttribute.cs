namespace BenchmarkDotNet.Attributes;

/// <summary>
/// Marks a <see cref="CancellationToken"/> property or field to be automatically injected with the benchmark's <see cref="CancellationToken"/>.
/// This allows benchmarks to cooperatively check for cancellation during long-running operations.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class BenchmarkCancellationAttribute : Attribute
{
}
