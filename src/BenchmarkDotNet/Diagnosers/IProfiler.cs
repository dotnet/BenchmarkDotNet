namespace BenchmarkDotNet.Diagnosers
{
    internal interface IProfiler : IDiagnoser
    {
        string ShortName { get; }
    }
}