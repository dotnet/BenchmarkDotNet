namespace BenchmarkDotNet.Diagnosers;

public class InProcessDiagnoserActionArgs(object benchmarkInstance)
{
    public object BenchmarkInstance { get; } = benchmarkInstance;
}