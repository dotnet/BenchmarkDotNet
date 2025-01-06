using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Locators;

public class FileLocatorArgs
{
    public FileLocatorArgs(BenchmarkCase benchmarkCase, ILogger logger)
    {
        BenchmarkCase = benchmarkCase;
        Logger = logger;
    }

    public BenchmarkCase BenchmarkCase { get; }
    public ILogger Logger { get; }
}