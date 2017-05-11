using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet
{
    public class DiagnosersLoader : IDiagnosersLoader
    {
        public IDiagnoser[] LoadDiagnosers() => new IDiagnoser[] { MemoryDiagnoser.Default };
    }
}