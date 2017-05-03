namespace BenchmarkDotNet.Diagnosers
{
    public interface IDiagnosersLoader
    {
        IDiagnoser[] LoadDiagnosers();
    }
}