using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains
{
    public interface IToolchain
    {
        [PublicAPI] string Name { get; }
        IGenerator Generator { get; }
        IBuilder Builder { get; }
        IExecutor Executor { get; }
        bool IsInProcess { get; }

        bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver);
    }
}