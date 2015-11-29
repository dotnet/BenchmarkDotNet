namespace BenchmarkDotNet.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
    }
}