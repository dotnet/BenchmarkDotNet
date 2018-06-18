using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Toolchains
{
    public interface IToolchain
    {
        string Name { get; }
        IGenerator Generator { get; }
        IBuilder Builder { get; }
        IExecutor Executor { get; }

        bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver);
    }
}