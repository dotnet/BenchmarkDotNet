using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes;

/// <inheritdoc cref="RunMode.ConsumeTasksSynchronously"/>
public class ConsumeTasksSynchronouslyAttribute(bool value) : JobMutatorConfigBaseAttribute(Job.Default.WithConsumeTasksSynchronously(value))
{
}
