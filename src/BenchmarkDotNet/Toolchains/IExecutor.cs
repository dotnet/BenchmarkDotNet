using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains;

public interface IExecutor
{
    ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters);
}