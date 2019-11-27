namespace BenchmarkDotNet.Diagnosers
{
    public interface IProfiler : IDiagnoser
    {
        string ShortName { get; }
    }
}