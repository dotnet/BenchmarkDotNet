using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains;

public interface IExecutor
{
    ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters, CancellationToken cancellationToken);
}