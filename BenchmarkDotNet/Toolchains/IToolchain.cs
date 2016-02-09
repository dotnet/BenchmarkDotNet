namespace BenchmarkDotNet.Toolchains
{
    public interface IToolchain
    {
        string Name { get; }
        IGenerator Generator { get; }
        IBuilder Builder { get; }
        IExecutor Executor { get; }
    }
}