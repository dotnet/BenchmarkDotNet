using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains;

public interface IToolchain
{
    Runtime Runtime { get; }
    IGenerator Generator { get; }
    IBuilder Builder { get; }
    IExecutor Executor { get; }
    bool IsInProcess { get; }

    IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver);
}