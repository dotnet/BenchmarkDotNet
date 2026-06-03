using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes;

/// <inheritdoc cref="RunMode.JitTieringMode"/>
public class JitTieringAttribute(JitTieringMode mode) : JobMutatorConfigBaseAttribute(Job.Default.WithJitTieringMode(mode))
{
}
