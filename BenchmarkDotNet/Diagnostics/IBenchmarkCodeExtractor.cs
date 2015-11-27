namespace BenchmarkDotNet.Diagnostics
{
    /// <summary>
    /// This is the interface that we expect the BenchmarkDotNet.Diagnostics "plugin" to implement
    /// </summary>
    public interface IBenchmarkCodeExtractor
    {
        void PrintCodeForMethod(bool printAssembly, bool printIL, bool printDiagnostics);
    }
}
