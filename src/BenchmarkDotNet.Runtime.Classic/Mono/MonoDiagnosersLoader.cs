using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Mono
{
    public class MonoDiagnosersLoader : IDiagnosersLoader
    {
        // this method should return a IHardwareCountersDiagnoser when we implement Hardware Counters for Unix
        public IDiagnoser[] LoadDiagnosers() => new IDiagnoser[] { MemoryDiagnoser.Default }; 
    }
}